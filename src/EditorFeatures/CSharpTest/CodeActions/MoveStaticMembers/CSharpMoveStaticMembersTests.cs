// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.MoveStaticMembers;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.MoveStaticMembers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.MoveStaticMembers;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MoveStaticMembers;

using VerifyCS = CSharpCodeRefactoringVerifier<
    CSharpMoveStaticMembersRefactoringProvider>;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
public sealed class CSharpMoveStaticMembersTests
{
    private static readonly TestComposition s_testServices = FeaturesTestCompositions.Features.AddParts(typeof(TestMoveStaticMembersService));

    #region Perform New Type Action From Options
    [Fact]
    public async Task TestMoveField()
    {
        var selectedMembers = ImmutableArray.Create("TestField");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static int Test[||]Field = 1;
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int TestField = 1;
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveProperty()
    {
        var selectedMembers = ImmutableArray.Create("TestProperty");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static int Test[||]Property { get; set; }
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int TestProperty { get; set; }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveEvent()
    {
        var selectedMembers = ImmutableArray.Create("TestEvent");
        await TestMovementNewFileAsync("""
            using System;

            namespace TestNs1
            {
                public class Class1
                {
                    public static event EventHandler Test[||]Event;
                }
            }
            """, """
            using System;

            namespace TestNs1
            {
                public class Class1
                {
                }
            }
            """, """
            using System;

            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static event EventHandler TestEvent;
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveMethod()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static int Test[||]Method()
                    {
                        return 0;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int TestMethod()
                    {
                        return 0;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveExtensionMethod()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public static class Class1
                {
                    public static int Test[||]Method(this Other other)
                    {
                        return other.OtherInt + 2;
                    }
                }

                public class Other
                {
                    public int OtherInt;
                    public Other()
                    {
                        OtherInt = 5;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public static class Class1
                {
                }

                public class Other
                {
                    public int OtherInt;
                    public Other()
                    {
                        OtherInt = 5;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int TestMethod(this Other other)
                    {
                        return other.OtherInt + 2;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveConstField()
    {
        var selectedMembers = ImmutableArray.Create("TestField");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public const int Test[||]Field = 1;
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public const int TestField = 1;
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveNothing()
    {
        var selectedMembers = ImmutableArray<string>.Empty;
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static int Test[||]Method()
                    {
                        return 0;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                    public static int Test[||]Method()
                    {
                        return 0;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveMethodWithTrivia()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                // some comment we don't want to move
                public class Class1
                {
                    // some comment we want to move
                    public static int Test[||]Method()
                    {
                        return 0;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                // some comment we don't want to move
                public class Class1
                {
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    // some comment we want to move
                    public static int TestMethod()
                    {
                        return 0;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveMultipleMethods()
    {
        var selectedMembers = ImmutableArray.Create("TestMethodInt", "TestMethodBool");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static bool TestMethodBool()
                    {
                        return false;
                    }

                    public static int Test[||]MethodInt()
                    {
                        return 0;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static bool TestMethodBool()
                    {
                        return false;
                    }

                    public static int TestMethodInt()
                    {
                        return 0;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveSingleMethodFromMultiple()
    {
        var selectedMembers = ImmutableArray.Create("TestMethodBool");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static int Test[||]MethodInt()
                    {
                        return 0;
                    }

                    public static bool TestMethodBool()
                    {
                        return false;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                    public static int TestMethodInt()
                    {
                        return 0;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {

                    public static bool TestMethodBool()
                    {
                        return false;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveOneOfEach()
    {
        var selectedMembers = ImmutableArray.Create(
            "TestMethod",
            "TestField",
            "TestProperty",
            "TestEvent");
        await TestMovementNewFileAsync("""
            using System;

            namespace TestNs1
            {
                public class Class1
                {
                    public static int TestField;

                    public static bool TestProperty { get; set; }

                    public static event EventHandler TestEvent;

                    public static int Test[||]Method()
                    {
                        return 0;
                    }
                }
            }
            """, """
            using System;

            namespace TestNs1
            {
                public class Class1
                {
                }
            }
            """, """
            using System;

            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int TestField;

                    public static bool TestProperty { get; set; }

                    public static event EventHandler TestEvent;

                    public static int Test[||]Method()
                    {
                        return 0;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestInNestedClass()
    {
        var selectedMembers = ImmutableArray.Create("TestField");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public class NestedClass1
                    {
                        public static int Test[||]Field = 1;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                    public class NestedClass1
                    {
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int TestField = 1;
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestInNestedNamespace()
    {
        var selectedMembers = ImmutableArray.Create("TestField");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                namespace InnerNs
                {
                    public class Class1
                    {
                        public static int Test[||]Field = 1;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                namespace InnerNs
                {
                    public class Class1
                    {
                    }
                }
            }
            """, """
            namespace TestNs1.InnerNs
            {
                internal static class Class1Helpers
                {
                    public static int TestField = 1;
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveFieldNoNamespace()
    {
        var selectedMembers = ImmutableArray.Create("TestField");
        await TestMovementNewFileAsync("""
            public class Class1
            {
                public static int Test[||]Field = 1;
            }
            """, """
            public class Class1
            {
            }
            """, """
            internal static class Class1Helpers
            {
                public static int TestField = 1;
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveFieldNewNamespace()
    {
        var selectedMembers = ImmutableArray.Create("TestField");
        await TestMovementNewFileAsync("""
            public class Class1
            {
                public static int Test[||]Field = 1;
            }
            """, """
            public class Class1
            {
            }
            """, """
            namespace NewNs
            {
                internal static class Class1Helpers
                {
                    public static int TestField = 1;
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "NewNs.Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveMethodWithNamespacedSelectedDestination()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static int Test[||]Method()
                    {
                        return 0;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                }
            }
            """, """
            namespace TestNs1.ExtraNs
            {
                internal static class Class1Helpers
                {
                    public static int TestMethod()
                    {
                        return 0;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "ExtraNs.Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveMethodFileScopedNamespace()
    {
        // We still keep normal namespacing rules in the new file
        var newFileName = "Class1Helpers.cs";
        var selectedMembers = ImmutableArray.Create("TestMethod");
        var expectedResult1 = """
            namespace TestNs1;

            public class Class1
            {
            }
            """;
        await new Test("Class1Helpers", selectedMembers, newFileName)
        {
            TestCode = """
            namespace TestNs1;

            public class Class1
            {
                public static int Test[||]Method()
                {
                    return 0;
                }
            }
            """,
            FixedState =
            {
                Sources =
                {
                    expectedResult1,
                    (newFileName, """
                    namespace TestNs1
                    {
                        internal static class Class1Helpers
                        {
                            public static int TestMethod()
                            {
                                return 0;
                            }
                        }
                    }
                    """)
                }
            },
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp10
        }.RunAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveGenericMethod()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static T Test[||]Method<T>(T item)
                    {
                        return item;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static T TestMethod<T>(T item)
                    {
                        return item;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveMethodWithGenericClass()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1<T>
                {
                    public static T Test[||]Method(T item)
                    {
                        return item;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1<T>
                {
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers<T>
                {
                    public static T Test[||]Method(T item)
                    {
                        return item;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveMethodAndRefactorUsage()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static int Test[||]Method()
                    {
                        return 0;
                    }
                }

                public class Class2
                {
                    public static int TestMethod2()
                    {
                        return Class1.TestMethod();
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                }

                public class Class2
                {
                    public static int TestMethod2()
                    {
                        return Class1Helpers.TestMethod();
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int TestMethod()
                    {
                        return 0;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79806")]
    public async Task TestMoveStaticMethodWithStaticMembers()
    {
        var selectedMembers = ImmutableArray.Create("StaticMethod");

        await TestMovementNewFileAsync("""
            using System;

            namespace TestNs1
            {
                internal class ClassWithStaticMembers
                {
                    public static int StaticInt { get; set; }
                    public static string StaticString { get; set; }
                    public static void Static[||]Method()
                    {
                        Console.WriteLine(StaticString + StaticInt);
                    }
                }
            }
            """, """
            using System;
            
            namespace TestNs1
            {
                internal class ClassWithStaticMembers
                {
                    public static int StaticInt { get; set; }
                    public static string StaticString { get; set; }
                }
            }
            """, """
            using System;
            
            namespace TestNs1
            {
                internal static class ClassWithStaticMembersHelpers
                {
                    public static void StaticMethod()
                    {
                        Console.WriteLine(ClassWithStaticMembers.StaticString + ClassWithStaticMembers.StaticInt);
                    }
                }
            }
            """, "ClassWithStaticMembersHelpers.cs", selectedMembers, "ClassWithStaticMembersHelpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveMethodAndRefactorUsageWithTrivia()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static int Test[||]Method()
                    {
                        return 0;
                    }
                }

                public class Class2
                {
                    public static int TestMethod2()
                    {
                        // keep this comment, and the random spaces here
                        return Class1. TestMethod( );
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                }

                public class Class2
                {
                    public static int TestMethod2()
                    {
                        // keep this comment, and the random spaces here
                        return Class1Helpers. TestMethod( );
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int TestMethod()
                    {
                        return 0;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveMethodAndRefactorSourceUsage()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static int Test[||]Method()
                    {
                        return 0;
                    }

                    public static int TestMethod2()
                    {
                        return TestMethod();
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                    public static int TestMethod2()
                    {
                        return Class1Helpers.TestMethod();
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int TestMethod()
                    {
                        return 0;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveFieldAndRefactorSourceUsage()
    {
        var selectedMembers = ImmutableArray.Create("TestField");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static int Test[||]Field = 0;

                    public static int TestMethod2()
                    {
                        return TestField;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                    public static int TestMethod2()
                    {
                        return Class1Helpers.TestField;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int TestField = 0;
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMovePropertyAndRefactorSourceUsage()
    {
        var selectedMembers = ImmutableArray.Create("TestProperty", "_testProperty");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    private static int _testProperty;

                    public static int Test[||]Property
                    {
                        get => _testProperty;
                        set
                        {
                            _testProperty = value;
                        }
                    }

                    public static int TestMethod2()
                    {
                        return TestProperty;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                    public static int TestMethod2()
                    {
                        return Class1Helpers.TestProperty;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    private static int _testProperty;

                    public static int Test[||]Property
                    {
                        get => _testProperty;
                        set
                        {
                            _testProperty = value;
                        }
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveGenericMethodAndRefactorImpliedUsage()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static T Test[||]Method<T>(T item)
                    {
                        return item;
                    }
                }

                public class Class2
                {
                    public static int TestMethod2()
                    {
                        return Class1.TestMethod(5);
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                }

                public class Class2
                {
                    public static int TestMethod2()
                    {
                        return Class1Helpers.TestMethod(5);
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static T TestMethod<T>(T item)
                    {
                        return item;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveGenericMethodAndRefactorUsage()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementNewFileAsync("""
            using System;

            namespace TestNs1
            {
                public class Class1
                {
                    public static Type Test[||]Method<T>()
                    {
                        return typeof(T);
                    }
                }

                public class Class2
                {
                    public static Type TestMethod2()
                    {
                        return Class1.TestMethod<int>();
                    }
                }
            }
            """, """
            using System;

            namespace TestNs1
            {
                public class Class1
                {
                }

                public class Class2
                {
                    public static Type TestMethod2()
                    {
                        return Class1Helpers.TestMethod<int>();
                    }
                }
            }
            """, """
            using System;

            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static Type TestMethod<T>()
                    {
                        return typeof(T);
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveMethodFromGenericClassAndRefactorUsage()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod", "TestGeneric");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1<T>
                {
                    public static T TestGeneric { get; set; }    

                    public static T Test[||]Method()
                    {
                        return TestGeneric;
                    }
                }

                public class Class2
                {
                    public static int TestMethod2()
                    {
                        return Class1<int>.TestMethod();
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1<T>
                {
                }

                public class Class2
                {
                    public static int TestMethod2()
                    {
                        return Class1Helpers<int>.TestMethod();
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers<T>
                {
                    public static T TestGeneric { get; set; }

                    public static T TestMethod()
                    {
                        return TestGeneric;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveMethodFromGenericClassAndRefactorPartialTypeArgUsage()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1<T1, T2, T3>
                    where T1 : new()
                {
                    public static T1 Test[||]Method()
                    {
                        return new T1();
                    }

                    public static T2 TestGeneric2 { get; set; } 

                    public T3 TestGeneric3 { get; set; }
                }

                public class Class2
                {
                    public static int TestMethod2()
                    {
                        return Class1<int, string, double>.TestMethod();
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1<T1, T2, T3>
                    where T1 : new()
                {
                    public static T2 TestGeneric2 { get; set; } 

                    public T3 TestGeneric3 { get; set; }
                }

                public class Class2
                {
                    public static int TestMethod2()
                    {
                        return Class1Helpers<int>.TestMethod();
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers<T1> where T1 : new()
                {
                    public static T1 TestMethod()
                    {
                        return new T1();
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveMethodAndRefactorUsageDifferentNamespace()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static int Test[||]Method()
                    {
                        return 0;
                    }
                }
            }

            namespace TestNs2
            {
                using TestNs1;

                public class Class2
                {
                    public static int TestMethod2()
                    {
                        return Class1.TestMethod();
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                }
            }

            namespace TestNs2
            {
                using TestNs1;

                public class Class2
                {
                    public static int TestMethod2()
                    {
                        return Class1Helpers.TestMethod();
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int TestMethod()
                    {
                        return 0;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveMethodAndRefactorUsageNewNamespace()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static int Test[||]Method()
                    {
                        return 0;
                    }
                }

                public class Class2
                {
                    public static int TestMethod2()
                    {
                        return Class1.TestMethod();
                    }
                }
            }
            """, """
            using TestNs1.ExtraNs;

            namespace TestNs1
            {
                public class Class1
                {
                }

                public class Class2
                {
                    public static int TestMethod2()
                    {
                        return Class1Helpers.TestMethod();
                    }
                }
            }
            """, """
            namespace TestNs1.ExtraNs
            {
                internal static class Class1Helpers
                {
                    public static int TestMethod()
                    {
                        return 0;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "ExtraNs.Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveMethodAndRefactorUsageSeparateFile()
    {
        var initialMarkup1 = """
            namespace TestNs1
            {
                public class Class1
                {
                    public static int Test[||]Method()
                    {
                        return 0;
                    }
                }
            }
            """;
        var initialMarkup2 = """
            using TestNs1;

            public class Class2
            {
                public static int TestMethod2()
                {
                    return Class1.TestMethod();
                }
            }
            """;
        var newFileName = "Class1Helpers.cs";
        var selectedMembers = ImmutableArray.Create("TestMethod");
        var expectedResult1 = """
            namespace TestNs1
            {
                public class Class1
                {
                }
            }
            """;
        var expectedResult3 = """
            using TestNs1;

            public class Class2
            {
                public static int TestMethod2()
                {
                    return Class1Helpers.TestMethod();
                }
            }
            """;
        await new Test("Class1Helpers", selectedMembers, newFileName)
        {
            TestState =
            {
                Sources =
                {
                    initialMarkup1,
                    initialMarkup2
                }
            },
            FixedState =
            {
                Sources =
                {
                    expectedResult1,
                    expectedResult3,
                    (newFileName, """
                    namespace TestNs1
                    {
                        internal static class Class1Helpers
                        {
                            public static int TestMethod()
                            {
                                return 0;
                            }
                        }
                    }
                    """)
                }
            }
        }.RunAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveMethodAndRefactorClassAlias()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static int Test[||]Method()
                    {
                        return 0;
                    }
                }
            }

            namespace TestNs2
            {
                using C1 = TestNs1.Class1;

                class Class2
                {
                    public static int TestMethod2()
                    {
                        return C1.TestMethod();
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                }
            }

            namespace TestNs2
            {
                using TestNs1;
                using C1 = TestNs1.Class1;

                class Class2
                {
                    public static int TestMethod2()
                    {
                        return Class1Helpers.TestMethod();
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int TestMethod()
                    {
                        return 0;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveMethodAndRefactorNamespaceAlias()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static int Test[||]Method()
                    {
                        return 0;
                    }
                }
            }

            namespace TestNs2
            {
                using C1 = TestNs1;

                class Class2
                {
                    public static int TestMethod2()
                    {
                        return C1.Class1.TestMethod();
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                }
            }

            namespace TestNs2
            {
                using TestNs1;
                using C1 = TestNs1;

                class Class2
                {
                    public static int TestMethod2()
                    {
                        return Class1Helpers.TestMethod();
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int TestMethod()
                    {
                        return 0;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveMethodAndRefactorConflictingName()
    {
        var newFileName = "Class1Helpers.cs";
        var selectedMembers = ImmutableArray.Create("Foo");
        var expectedResult1 = """
            namespace TestNs1
            {
                public class Class1
                {
                }
            }

            namespace TestNs2
            {
                using TestNs1;

                class Class2
                {
                    class Class1Helpers
                    {
                        public static int Foo()
                        {
                            return 1;
                        }
                    }

                    public static int TestMethod2()
                    {
                        return TestNs1.Class1Helpers.Foo() + Class1Helpers.Foo();
                    }
                }
            }
            """;
        await new Test("Class1Helpers", selectedMembers, newFileName)
        {
            TestCode = """
            namespace TestNs1
            {
                public class Class1
                {
                    public static int F[||]oo()
                    {
                        return 0;
                    }
                }
            }

            namespace TestNs2
            {
                using TestNs1;

                class Class2
                {
                    class Class1Helpers
                    {
                        public static int Foo()
                        {
                            return 1;
                        }
                    }

                    public static int TestMethod2()
                    {
                        return Class1.Foo() + Class1Helpers.Foo();
                    }
                }
            }
            """,
            FixedState =
            {
                Sources =
                {
                    expectedResult1,
                    (newFileName, """
                    namespace TestNs1
                    {
                        internal static class Class1Helpers
                        {
                            public static int Foo()
                            {
                                return 0;
                            }
                        }
                    }
                    """)
                }
            },
            // the test parser thinks "TestNs1.Class1Helpers" is a member access expression
            // but we made a qualified name. The text should still be the same
            CodeActionValidationMode = Testing.CodeActionValidationMode.None
        }.RunAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveMethodAndRefactorQualifiedName()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static int Test[||]Method()
                    {
                        return 0;
                    }
                }
            }

            namespace TestNs2
            {
                class Class2
                {
                    public static int TestMethod2()
                    {
                        return TestNs1.Class1.TestMethod();
                    }
                }
            }
            """, """
            using TestNs1;

            namespace TestNs1
            {
                public class Class1
                {
                }
            }

            namespace TestNs2
            {
                class Class2
                {
                    public static int TestMethod2()
                    {
                        return Class1Helpers.TestMethod();
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int TestMethod()
                    {
                        return 0;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveMethodAndRefactorStaticUsing()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static int Test[||]Method()
                    {
                        return 0;
                    }
                }
            }

            namespace TestNs2
            {
                using static TestNs1.Class1;

                class Class2
                {
                    public static int TestMethod2()
                    {
                        return TestMethod();
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                }
            }

            namespace TestNs2
            {
                using TestNs1;
                using static TestNs1.Class1;

                class Class2
                {
                    public static int TestMethod2()
                    {
                        return Class1Helpers.TestMethod();
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int TestMethod()
                    {
                        return 0;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveMethodAndRefactorNamespaceAliasWithExtraNamespace()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static int Test[||]Method()
                    {
                        return 0;
                    }
                }
            }

            namespace TestNs2
            {
                using C1 = TestNs1;

                class Class2
                {
                    public static int TestMethod2()
                    {
                        return C1.Class1.TestMethod();
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                }
            }

            namespace TestNs2
            {
                using TestNs1.ExtraNs;
                using C1 = TestNs1;

                class Class2
                {
                    public static int TestMethod2()
                    {
                        return Class1Helpers.TestMethod();
                    }
                }
            }
            """, """
            namespace TestNs1.ExtraNs
            {
                internal static class Class1Helpers
                {
                    public static int TestMethod()
                    {
                        return 0;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "ExtraNs.Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveExtensionMethodDoNotRefactor()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public static class Class1
                {
                    public static int Test[||]Method(this Other other)
                    {
                        return other.OtherInt + 2;
                    }
                }

                public class Class2
                {
                    public int GetOtherInt()
                    {
                        var other = new Other();
                        return other.TestMethod();
                    }
                }

                public class Other
                {
                    public int OtherInt;
                    public Other()
                    {
                        OtherInt = 5;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public static class Class1
                {
                }

                public class Class2
                {
                    public int GetOtherInt()
                    {
                        var other = new Other();
                        return other.TestMethod();
                    }
                }

                public class Other
                {
                    public int OtherInt;
                    public Other()
                    {
                        OtherInt = 5;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int TestMethod(this Other other)
                    {
                        return other.OtherInt + 2;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveExtensionMethodRefactorImports()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                using TestNs2;

                public static class Class1
                {
                    public static int Test[||]Method(this Other other)
                    {
                        return other.OtherInt + 2;
                    }
                }
            }

            namespace TestNs2
            {
                using TestNs1;

                public class Class2
                {
                    public int GetOtherInt()
                    {
                        var other = new Other();
                        return other.TestMethod();
                    }
                }

                public class Other
                {
                    public int OtherInt;
                    public Other()
                    {
                        OtherInt = 5;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                using TestNs2;

                public static class Class1
                {
                }
            }

            namespace TestNs2
            {
                using TestNs1;
                using TestNs1.ExtraNs;

                public class Class2
                {
                    public int GetOtherInt()
                    {
                        var other = new Other();
                        return other.TestMethod();
                    }
                }

                public class Other
                {
                    public int OtherInt;
                    public Other()
                    {
                        OtherInt = 5;
                    }
                }
            }
            """, """
            using TestNs2;

            namespace TestNs1.ExtraNs
            {
                internal static class Class1Helpers
                {
                    public static int TestMethod(this Other other)
                    {
                        return other.OtherInt + 2;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "ExtraNs.Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveExtensionMethodRefactorMultipleImports()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                using TestNs2;

                public static class Class1
                {
                    public static int Test[||]Method(this Other other)
                    {
                        return other.OtherInt + 2;
                    }
                }
            }

            namespace TestNs2
            {
                using TestNs1;

                public class Class2
                {
                    public int GetOtherInt()
                    {
                        var other = new Other();
                        return other.TestMethod();
                    }

                    public int GetOtherInt2()
                    {
                        var other = new Other();
                        return other.TestMethod();
                    }
                }

                public class Other
                {
                    public int OtherInt;
                    public Other()
                    {
                        OtherInt = 5;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                using TestNs2;

                public static class Class1
                {
                }
            }

            namespace TestNs2
            {
                using TestNs1;
                using TestNs1.ExtraNs;

                public class Class2
                {
                    public int GetOtherInt()
                    {
                        var other = new Other();
                        return other.TestMethod();
                    }

                    public int GetOtherInt2()
                    {
                        var other = new Other();
                        return other.TestMethod();
                    }
                }

                public class Other
                {
                    public int OtherInt;
                    public Other()
                    {
                        OtherInt = 5;
                    }
                }
            }
            """, """
            using TestNs2;

            namespace TestNs1.ExtraNs
            {
                internal static class Class1Helpers
                {
                    public static int TestMethod(this Other other)
                    {
                        return other.OtherInt + 2;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "ExtraNs.Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveMethodFromStaticClass()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public static class Class1
                {
                    public static int Test[||]Method()
                    {
                        return 0;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public static class Class1
                {
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int TestMethod()
                    {
                        return 0;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveMethodRetainFileBanner()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementNewFileAsync("""
            // Here is an example of a license or something
            // we would want to keep at the top of a file

            namespace TestNs1
            {
                public static class Class1
                {
                    public static int Test[||]Method()
                    {
                        return 0;
                    }
                }
            }
            """, """
            // Here is an example of a license or something
            // we would want to keep at the top of a file

            namespace TestNs1
            {
                public static class Class1
                {
                }
            }
            """, """
            // Here is an example of a license or something
            // we would want to keep at the top of a file

            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int TestMethod()
                    {
                        return 0;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }
    #endregion

    #region Perform Existing Type Action From Options
    [Fact]
    public async Task TestMoveFieldToExistingType()
    {
        var selectedMembers = ImmutableArray.Create("TestField");
        await TestMovementExistingFileAsync(
            """
            public class Class1
            {
                public static int Test[||]Field = 1;
            }
            """,
            """
            public class Class1Helpers
            {
            }
            """,
            """
            public class Class1
            {
            }
            """,
            """
            public class Class1Helpers
            {
                public static int TestField = 1;
            }
            """,
            selectedMembers,
            "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMovePropertyToExistingType()
    {
        var selectedMembers = ImmutableArray.Create("TestProperty");
        await TestMovementExistingFileAsync(
            """
            public class Class1
            {
                public static int Test[||]Property { get; set; }
            }
            """,
            """
            public class Class1Helpers
            {
            }
            """,
            """
            public class Class1
            {
            }
            """,
            """
            public class Class1Helpers
            {
                public static int TestProperty { get; set; }
            }
            """,
            selectedMembers,
            "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveEventToExistingType()
    {
        var selectedMembers = ImmutableArray.Create("TestEvent");
        await TestMovementExistingFileAsync(
            """
            using System;

            public class Class1
            {
                public static event EventHandler Test[||]Event;
            }
            """,
            """
            public class Class1Helpers
            {
            }
            """,
            """
            using System;

            public class Class1
            {
            }
            """,
            """
            using System;

            public class Class1Helpers
            {
                public static event EventHandler TestEvent;
            }
            """,
            selectedMembers,
            "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveMethodToExistingType()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementExistingFileAsync(
            """
            public class Class1
            {
                public static int Test[||]Method()
                {
                    return 0;
                }
            }
            """,
            """
            public class Class1Helpers
            {
            }
            """,
            """
            public class Class1
            {
            }
            """,
            """
            public class Class1Helpers
            {
                public static int TestMethod()
                {
                    return 0;
                }
            }
            """,
            selectedMembers,
            "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveExtensionMethodToExistingType()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementExistingFileAsync(
            """
            public static class Class1
            {
                public static int Test[||]Method(this Other other)
                {
                    return other.OtherInt + 2;
                }
            }

            public class Other
            {
                public int OtherInt;
                public Other()
                {
                    OtherInt = 5;
                }
            }
            """,
            """
            public static class Class1Helpers
            {
            }
            """,
            """
            public static class Class1
            {
            }

            public class Other
            {
                public int OtherInt;
                public Other()
                {
                    OtherInt = 5;
                }
            }
            """,
            """
            public static class Class1Helpers
            {
                public static int TestMethod(this Other other)
                {
                    return other.OtherInt + 2;
                }
            }
            """,
            selectedMembers,
            "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveConstFieldToExistingType()
    {
        var selectedMembers = ImmutableArray.Create("TestField");
        await TestMovementExistingFileAsync(
            """
            public class Class1
            {
                public const int Test[||]Field = 1;
            }
            """,
            """
            public class Class1Helpers
            {
            }
            """,
            """
            public class Class1
            {
            }
            """,
            """
            public class Class1Helpers
            {
                public const int TestField = 1;
            }
            """,
            selectedMembers,
            "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveMethodToExistingTypeWithNamespace()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementExistingFileAsync(
            """
            namespace TestNs
            {
                public class Class1
                {
                    public static int Test[||]Method()
                    {
                        return 0;
                    }
                }
            }
            """,
            """
            namespace TestNs
            {
                public class Class1Helpers
                {
                }
            }
            """,
            """
            namespace TestNs
            {
                public class Class1
                {
                }
            }
            """,
            """
            namespace TestNs
            {
                public class Class1Helpers
                {
                    public static int TestMethod()
                    {
                        return 0;
                    }
                }
            }
            """,
            selectedMembers,
            "TestNs.Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveMethodToExistingTypeWithNewNamespace()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementExistingFileAsync(
            """
            public class Class1
            {
                public static int Test[||]Method()
                {
                    return 0;
                }
            }
            """,
            """
            namespace TestNs
            {
                public class Class1Helpers
                {
                }
            }
            """,
            """
            public class Class1
            {
            }
            """,
            """
            namespace TestNs
            {
                public class Class1Helpers
                {
                    public static int TestMethod()
                    {
                        return 0;
                    }
                }
            }
            """,
            selectedMembers,
            "TestNs.Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveMethodToExistingTypeRefactorSourceUsage()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementExistingFileAsync(
            """
            public class Class1
            {
                public static int Test[||]Method()
                {
                    return 0;
                }

                public static int TestMethod2()
                {
                    return TestMethod();
                }
            }
            """,
            """
            public class Class1Helpers
            {
            }
            """,
            """
            public class Class1
            {
                public static int TestMethod2()
                {
                    return Class1Helpers.TestMethod();
                }
            }
            """,
            """
            public class Class1Helpers
            {
                public static int TestMethod()
                {
                    return 0;
                }
            }
            """,
            selectedMembers,
            "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestMoveMethodToExistingTypeRefactorDestinationUsage()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementExistingFileAsync(
            """
            public class Class1
            {
                public static int Test[||]Method()
                {
                    return 0;
                }
            }
            """,
            """
            public class Class1Helpers
            {
                public static int TestMethod2()
                {
                    return Class1.TestMethod();
                }
            }
            """,
            """
            public class Class1
            {
            }
            """,
            """
            public class Class1Helpers
            {
                public static int TestMethod()
                {
                    return 0;
                }
                public static int TestMethod2()
                {
                    return Class1Helpers.TestMethod();
                }
            }
            """,
            selectedMembers,
            "Class1Helpers").ConfigureAwait(false);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66734")]
    public async Task TestMoveMethodToExistingTypeInSameFile()
    {
        var selectedMembers = ImmutableArray.Create("IsValidWorkflowType", "validWorkflowTypes");
        await TestMovementExistingFileAsync(
            """
            public static class WorkflowTypes
            {
                public const string FirstType = "firstType";
            }

            public static class WorkflowValidations
            {
                private static readonly System.Collections.Generic.List<string> validWorkflowTypes = new System.Collections.Generic.List<string>()
                {
                    "firstType"
                };

            //  Moving this method and above dependency into WorkflowTypes static class 
                public static bool IsValid[||]WorkflowType(this string workflowType) => validWorkflowTypes.Contains(workflowType);
            }
            """,
            // We're testing a move inside the same file, so just use an empty destination.
            initialDestinationMarkup: string.Empty,
            """
            public static class WorkflowTypes
            {
                public const string FirstType = "firstType";
                private static readonly System.Collections.Generic.List<string> validWorkflowTypes = new System.Collections.Generic.List<string>()
                {
                    "firstType"
                };
            
            //  Moving this method and above dependency into WorkflowTypes static class 
                public static bool IsValidWorkflowType(this string workflowType) => validWorkflowTypes.Contains(workflowType);
            }

            public static class WorkflowValidations
            {
            }
            """,
            fixedDestinationMarkup: string.Empty,
            selectedMembers,
            "WorkflowTypes").ConfigureAwait(false);
    }

    #endregion

    #region Selections and caret position

    [Fact]
    public async Task TestSelectInMethodParens()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static int TestMethod([||])
                    {
                        return 0;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int TestMethod()
                    {
                        return 0;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectWholeFieldDeclaration()
    {
        var selectedMembers = ImmutableArray.Create("TestField");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    [|public static int TestField = 1;|]
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int TestField = 1;
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectBeforeKeywordOfDeclaration()
    {
        var selectedMembers = ImmutableArray.Create("TestField");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    [||]public static int TestField = 1;
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int TestField = 1;
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectInKeyWordOfDeclaration1()
    {
        var selectedMembers = ImmutableArray.Create("TestField");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    pub[||]lic static int TestField = 1;
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int TestField = 1;
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectInKeyWordOfDeclaration2()
    {
        var selectedMembers = ImmutableArray.Create("TestField");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public st[||]atic int TestField = 1;
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int TestField = 1;
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectInTypeIdentifierMethodDeclaration()
    {
        var selectedMembers = ImmutableArray.Create("TestMethod");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static i[||]nt TestMethod()
                    {
                        return 0;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int TestMethod()
                    {
                        return 0;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectInFieldInitializerAfterSemicolon()
    {
        var selectedMembers = ImmutableArray.Create("TestField");
        await TestMovementNewFileAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static int TestField = 1;[||]
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int TestField = 1;
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectInMultipleFieldIdentifiers()
    {
        var selectedMembers = ImmutableArray.Create("Goo", "Foo");
        await TestMovementNewFileWithSelectionAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    [|public static int Goo = 10, Foo = 9;|]
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int Goo = 10;
                    public static int Foo = 9;
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectMultipleMembers1()
    {
        var selectedMembers = ImmutableArray.Create("Goo", "Foo", "DoSomething");
        await TestMovementNewFileWithSelectionAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    [|public static int Goo = 10, Foo = 9;

                    public static int DoSomething()
                    {
                        return 5;
                    }|]
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int Goo = 10;
                    public static int Foo = 9;

                    public static int DoSomething()
                    {
                        return 5;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectMultipleMembers2()
    {
        var selectedMembers = ImmutableArray.Create("Goo", "Foo");
        await TestMovementNewFileWithSelectionAsync("""
            namespace TestNs1
            {
                public class Class1
                {

                    public static int DoSomething()
                    {
                        return [|5;
                    }        
                    public static int Goo = 10, Foo = 9;|]
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {

                    public static int DoSomething()
                    {
                        return 5;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int Goo = 10;
                    public static int Foo = 9;
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectMultipleMembers3()
    {
        var selectedMembers = ImmutableArray.Create("Goo", "Foo", "DoSomething");
        await TestMovementNewFileWithSelectionAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static int Go[|o = 10, Foo = 9;

                    public static int DoSometh|]ing()
                    {
                        return 5;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int Goo = 10;
                    public static int Foo = 9;

                    public static int DoSomething()
                    {
                        return 5;
                    }
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectMultipleMembers4()
    {
        var selectedMembers = ImmutableArray.Create("Foo");
        await TestMovementNewFileWithSelectionAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static int Goo = 10, F[|oo = 9;

                    public static in|]t DoSomething()
                    {
                        return 5;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                    public static int Goo = 10;

                    public static int DoSomething()
                    {
                        return 5;
                    }
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int Foo = 9;
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectOneOfMultipleFieldIdentifiers()
    {
        var selectedMembers = ImmutableArray.Create("Goo");
        await TestMovementNewFileWithSelectionAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static int G[||]oo = 10, Foo = 9;
                }
            }
            """, """
            namespace TestNs1
            {
                public class Class1
                {
                    public static int Foo = 9;
                }
            }
            """, """
            namespace TestNs1
            {
                internal static class Class1Helpers
                {
                    public static int Goo = 10;
                }
            }
            """, "Class1Helpers.cs", selectedMembers, "Class1Helpers").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectInTypeIdentifierOfFieldDeclaration_NoAction()
    {
        await TestNoRefactoringAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static i[||]nt TestField = 1;
                }
            }
            """).ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectInFieldInitializerEquals_NoAction()
    {
        // The initializer isn't a member declaration
        await TestNoRefactoringAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static int TestField =[||] 1;
                }
            }
            """).ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectMethodBody_NoAction()
    {
        await TestNoRefactoringAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static int TestMethod()
                    {
                        retu[||]rn 0;
                    }
                }
            }
            """).ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectMethodBracket_NoAction()
    {
        await TestNoRefactoringAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static int TestMethod()
                    [|{|]
                        return 0;
                    }
                }
            }
            """).ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectMalformedMethod_NoAction()
    {
        await new Test("", [], "")
        {
            TestCode = """
            namespace TestNs1
            {
                public class Class1
                {
                    public st[||] {|CS1519:int|} TestMethod()
                    {
                        return 0;
                    }
                }
            }
            """,
        }.RunAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectMalformedField_NoAction1()
    {
        await new Test("", [], "")
        {
            TestCode = """
            namespace TestNs1
            {
                public class Class1
                {
                    public st[||] {|CS1519:int|} TestField = 0;
                }
            }
            """,
        }.RunAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectMalformedField_NoAction2()
    {
        await new Test("", [], "")
        {
            TestCode = """
            namespace TestNs1
            {
                public class Class1
                {
                    public st [|{|CS1519:int|} Test|]Field = 0;
                }
            }
            """,
        }.RunAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectMalformedField_NoAction3()
    {
        await new Test("", [], "")
        {
            TestCode = """
            namespace TestNs1
            {
                public class Class1
                {
                    [|public st {|CS1519:int|} TestField = 0;|]
                }
            }
            """,
        }.RunAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectMalformedField_NoAction4()
    {
        await new Test("", [], "")
        {
            TestCode = """
            namespace TestNs1
            {
                public class Class1
                {
                    [|publicc {|CS1585:static|} int TestField = 0;|]
                }
            }
            """,
        }.RunAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectPropertyBody_NoAction()
    {
        await TestNoRefactoringAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public static int TestProperty { get; [||]set; }
                }
            }
            """).ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectNonStaticProperty_NoAction()
    {
        await TestNoRefactoringAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    public int Test[||]Property { get; set; }
                }
            }
            """).ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectStaticConstructor1_NoAction()
    {
        await TestNoRefactoringAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    [|static Class1()|]
                    {
                    }
                }
            }
            """).ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectStaticConstructor2_NoAction()
    {
        await TestNoRefactoringAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    static Cl[||]ass1()
                    {
                    }
                }
            }
            """).ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectOperator_NoAction()
    {
        await TestNoRefactoringAsync("""
            namespace TestNs1
            {
                public class Class1
                {
                    [|public static Class1 operator +(Class1 a, Class1 b)|]
                    {
                        return new Class1();
                    }
                }
            }
            """).ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectTopLevelStatement_NoAction1()
    {
        await new Test("", [], "")
        {
            TestCode = """
            using System;

            [||]Console.WriteLine(5);
            """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp10,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication
            },
        }.RunAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectTopLevelStatement_NoAction2()
    {
        await new Test("", [], "")
        {
            TestCode = """
            using System;

            [|Console.WriteLine(5);|]
            """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp10,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication
            },
        }.RunAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task TestSelectTopLevelLocalFunction_NoAction()
    {
        await new Test("", [], "")
        {
            TestCode = """
            DoSomething();

            static int Do[||]Something()
            {
                return 5;
            }
            """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp10,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication
            },
        }.RunAsync().ConfigureAwait(false);
    }
    #endregion

    private sealed class Test : VerifyCS.Test
    {
        public Test(
            string destinationType,
            ImmutableArray<string> selection,
            string? destinationName,
            bool testPreselection = false,
            bool createNew = true)
        {
            _destinationType = destinationType;
            _selection = selection;
            _destinationName = destinationName;
            _testPreselection = testPreselection;
            _createNew = createNew;
        }

        private readonly string _destinationType;

        private readonly ImmutableArray<string> _selection;

        private readonly string? _destinationName;

        private readonly bool _createNew;

        private readonly bool _testPreselection;

        protected override Task<Workspace> CreateWorkspaceImplAsync()
        {
            var hostServices = s_testServices.GetHostServices();

            var workspace = new AdhocWorkspace(hostServices);
            var testOptionsService = (TestMoveStaticMembersService)workspace.Services.GetRequiredService<IMoveStaticMembersOptionsService>();
            testOptionsService.DestinationName = _destinationType;
            testOptionsService.SelectedMembers = _selection;
            testOptionsService.Filename = _destinationName;
            testOptionsService.CreateNew = _createNew;
            testOptionsService.ExpectedPrecheckedMembers = _testPreselection ? _selection : [];

            return Task.FromResult<Workspace>(workspace);
        }
    }

    private static async Task TestMovementNewFileAsync(
        string initialMarkup,
        string expectedSource,
        string expectedNewFile,
        string newFileName,
        ImmutableArray<string> selectedMembers,
        string newTypeName)
        => await new Test(newTypeName, selectedMembers, newFileName)
        {
            TestCode = initialMarkup,
            FixedState =
            {
                Sources =
                {
                    expectedSource,
                    (newFileName, expectedNewFile)
                }
            },
        }.RunAsync().ConfigureAwait(false);

    private static async Task TestMovementNewFileWithSelectionAsync(
        string initialMarkup,
        string expectedSource,
        string expectedNewFile,
        string newFileName,
        ImmutableArray<string> selectedMembers,
        string newTypeName)
        => await new Test(newTypeName, selectedMembers, newFileName, testPreselection: true)
        {
            TestCode = initialMarkup,
            FixedState =
            {
                Sources =
                {
                    expectedSource,
                    (newFileName, expectedNewFile)
                }
            },
        }.RunAsync().ConfigureAwait(false);

    private static async Task TestMovementExistingFileAsync(
        string intialSourceMarkup,
        string initialDestinationMarkup,
        string fixedSourceMarkup,
        string fixedDestinationMarkup,
        ImmutableArray<string> selectedMembers,
        string selectedDestinationType,
        string? selectedDestinationFile = null)
    {
        var test = new Test(selectedDestinationType, selectedMembers, selectedDestinationFile, createNew: false);
        test.TestState.Sources.Add(intialSourceMarkup);
        test.FixedState.Sources.Add(fixedSourceMarkup);
        if (selectedDestinationFile != null)
        {
            test.TestState.Sources.Add((selectedDestinationFile, initialDestinationMarkup));
            test.FixedState.Sources.Add((selectedDestinationFile, fixedDestinationMarkup));
        }
        else
        {
            test.TestState.Sources.Add(initialDestinationMarkup);
            test.FixedState.Sources.Add(fixedDestinationMarkup);
        }

        await test.RunAsync().ConfigureAwait(false);
    }

    private static async Task TestNoRefactoringAsync(string initialMarkup)
    {
        await new Test("", [], "")
        {
            TestCode = initialMarkup,
        }.RunAsync().ConfigureAwait(false);
    }
}
