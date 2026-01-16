// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

[CompilerTrait(CompilerFeature.CollectionExpressions)]
public sealed class CollectionExpressionTests_WithElement_Nullable : CSharpTestBase
{
    private static string? IncludeExpectedOutput(string expectedOutput) => ExecutionConditionUtil.IsMonoOrCoreClr ? expectedOutput : null;

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72142")]
    public void ConstructorTestNullElementAdd()
    {
        var source = """
            #nullable enable
            using System;
            using System.Collections.Generic;

            List<string> list = [null];
            Console.WriteLine(list.Count);
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("1")).VerifyDiagnostics(
            // (4,22): warning CS8625: Cannot convert null literal to non-nullable reference type.
            // List<string> list = [null];
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(5, 22));
    }

    private void AssertExpectedWithElementSymbol(Compilation compilation, string expected)
    {
        var syntaxTree = compilation.SyntaxTrees.Last();
        var root = syntaxTree.GetRoot();
        var withElement = root.DescendantNodes().OfType<WithElementSyntax>().Single();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var symbolInfo = semanticModel.GetSymbolInfo(withElement);
        AssertEx.Equal(expected, symbolInfo.Symbol.ToTestDisplayString(includeNonNullable: true));
    }

    [Fact]
    public void ConstructorNonNullParameterPassedNull()
    {
        var source = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(string arg)
                {
                }
            }
            
            class C
            {
                static void Main()
                {
                    MyList<int> list = [with(null), 1, 2];
                    Console.WriteLine($"{list.Count}");
                }
            }
            """;

        var verifier = CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("2")).VerifyDiagnostics(
            // (16,34): warning CS8625: Cannot convert null literal to non-nullable reference type.
            //         MyList<int> list = [with(null), 1, 2];
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(16, 34));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.String! arg)");
    }

    [Fact]
    public void ConstructorNonNullParameterPassedPossibleNull()
    {
        var source = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(string arg)
                {
                }
            }
            
            class C
            {
                static void Main()
                {
                    string? s = null;
                    MyList<int> list = [with(s), 1, 2];
                    Console.WriteLine($"{list.Count}");
                }
            }
            """;

        var verifier = CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("2")).VerifyDiagnostics(
            // (17,34): warning CS8604: Possible null reference argument for parameter 'arg' in 'MyList<int>.MyList(string arg)'.
            //         MyList<int> list = [with(s), 1, 2];
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("arg", "MyList<int>.MyList(string arg)").WithLocation(17, 34));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.String! arg)");
    }

    [Fact]
    public void ConstructorNonNullParameterPassedSuppressedValue()
    {
        var source = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(string arg)
                {
                }
            }
            
            class C
            {
                static void Main()
                {
                    string? s = null;
                    MyList<int> list = [with(s!), 1, 2];
                    Console.WriteLine($"{list.Count}");
                }
            }
            """;

        var verifier = CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("2")).VerifyDiagnostics();
        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.String! arg)");
    }

    [Fact]
    public void ConstructorNonNullParameterPassedAssignedValue()
    {
        var source = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(string arg)
                {
                }
            }
            
            class C
            {
                static void Main()
                {
                    string? s = null;
                    MyList<int> list = [with(s = ""), 1, 2];
                    Console.WriteLine($"{list.Count}");
                }
            }
            """;

        var verifier = CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("2")).VerifyDiagnostics(
            // (16,17): warning CS0219: The variable 's' is assigned but its value is never used
            //         string? s = null;
            Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s").WithArguments("s").WithLocation(16, 17));
        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.String! arg)");
    }

    [Fact]
    public void ConstructorNonNullParameterPassedNull_Inference1()
    {
        var source = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(T arg)
                {
                }
            }
            
            class C
            {
                static void Main()
                {
                    Goo([with(null), ""]);
                }

                static void Goo<T>(MyList<T> list)
                {
                    Console.WriteLine(list.Count);
                }
            }
            """;

        var verifier = CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("1")).VerifyDiagnostics(
            // (16,19): warning CS8625: Cannot convert null literal to non-nullable reference type.
            //         Goo([with(null), ""]);
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(16, 19));
        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.String!>.MyList(System.String! arg)");
    }

    [Fact]
    public void ConstructorNonNullParameterPassedNull_Inference2()
    {
        var source = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            
            class MyList<T> : List<T>
            {
                public MyList(T? arg)
                {
                }
            }
            
            class C
            {
                static void Main()
                {
                    var val = Goo([with(null), ""]);
                    Bar(val);
                }

                static void Bar(string s) { }

                static T Goo<T>(MyList<T> list)
                {
                    Console.WriteLine(list.Count);
                    return default!;
                }
            }
            """;

        var verifier = CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("1")).VerifyDiagnostics();

        var compilation = verifier.Compilation;
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Last());

        var invocation = compilation.SyntaxTrees.Last().GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
        var symbolInfo = semanticModel.GetSymbolInfo(invocation);
        AssertEx.Equal("System.String! C.Goo<System.String!>(MyList<System.String!>! list)", symbolInfo.Symbol.ToTestDisplayString(true));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.String!>.MyList(System.String? arg)");
    }

    [Fact]
    public void CollectionBuilderNonNullParameterPassedNull()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : IEnumerable<T>
            {
                private readonly List<T> _items;
                public MyCollection(string value, ReadOnlySpan<T> items)
                {
                    _items = new();
                    _items.AddRange(items.ToArray());
                }
                public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>(string value, ReadOnlySpan<T> items) => new(value, items);
            }
            """;
        string sourceB = """
            #nullable enable
            using System;
            class Program
            {
                static void Main()
                {
                    MyCollection<int> c = [with(null), 1, 2];
                    Console.WriteLine(string.Join(", ", c));
                }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            expectedOutput: IncludeExpectedOutput("1, 2"),
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (7,37): warning CS8625: Cannot convert null literal to non-nullable reference type.
            //         MyCollection<int> c = [with(null), 1, 2];
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(7, 37));
        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.String! value, System.ReadOnlySpan<System.Int32> items)");
    }

    [Fact]
    public void CollectionBuilderNonNullParameterPassedPossibleNull()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : IEnumerable<T>
            {
                private readonly List<T> _items;
                public MyCollection(string value, ReadOnlySpan<T> items)
                {
                    _items = new();
                    _items.AddRange(items.ToArray());
                }
                public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>(string value, ReadOnlySpan<T> items) => new(value, items);
            }
            """;
        string sourceB = """
            #nullable enable
            using System;
            class Program
            {
                static void Main()
                {
                    string? s = null;
                    MyCollection<int> c = [with(s), 1, 2];
                    Console.WriteLine(string.Join(", ", c));
                }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            expectedOutput: IncludeExpectedOutput("1, 2"),
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (8,37): warning CS8604: Possible null reference argument for parameter 'value' in 'MyCollection<int> MyBuilder.Create<int>(string value, ReadOnlySpan<int> items)'.
            //         MyCollection<int> c = [with(s), 1, 2];
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("value", "MyCollection<int> MyBuilder.Create<int>(string value, ReadOnlySpan<int> items)").WithLocation(8, 37));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.String! value, System.ReadOnlySpan<System.Int32> items)");
    }

    [Fact]
    public void CollectionBuilderNonNullParameterPassedSuppressedValue()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : IEnumerable<T>
            {
                private readonly List<T> _items;
                public MyCollection(string value, ReadOnlySpan<T> items)
                {
                    _items = new();
                    _items.AddRange(items.ToArray());
                }
                public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>(string value, ReadOnlySpan<T> items) => new(value, items);
            }
            """;
        string sourceB = """
            #nullable enable
            using System;
            class Program
            {
                static void Main()
                {
                    string? s = null;
                    MyCollection<int> c = [with(s!), 1, 2];
                    Console.WriteLine(string.Join(", ", c));
                }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            expectedOutput: IncludeExpectedOutput("1, 2"),
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics();

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.String! value, System.ReadOnlySpan<System.Int32> items)");
    }

    [Fact]
    public void CollectionBuilderNonNullParameterPassedAssignedValue()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : IEnumerable<T>
            {
                private readonly List<T> _items;
                public MyCollection(string value, ReadOnlySpan<T> items)
                {
                    _items = new();
                    _items.AddRange(items.ToArray());
                }
                public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>(string value, ReadOnlySpan<T> items) => new(value, items);
            }
            """;
        string sourceB = """
            #nullable enable
            using System;
            class Program
            {
                static void Main()
                {
                    string? s = null;
                    MyCollection<int> c = [with(s = ""), 1, 2];
                    Console.WriteLine(string.Join(", ", c));
                }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            expectedOutput: IncludeExpectedOutput("1, 2"),
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (7,17): warning CS0219: The variable 's' is assigned but its value is never used
            //         string? s = null;
            Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s").WithArguments("s").WithLocation(7, 17));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.String! value, System.ReadOnlySpan<System.Int32> items)");
    }

    [Fact]
    public void CollectionBuilderNonNullParameterPassedNull_Inference1()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : IEnumerable<T>
            {
                private readonly List<T> _items;
                public MyCollection(T value, ReadOnlySpan<T> items)
                {
                    _items = new();
                    _items.AddRange(items.ToArray());
                }
                public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>(T value, ReadOnlySpan<T> items) => new(value, items);
            }
            """;
        string sourceB = """
            #nullable enable
            using System;
            class Program
            {
                static void Main()
                {
                    Goo([with(null), "goo"]);
                }

                static void Goo<T>(MyCollection<T> list)
                {
                    Console.WriteLine(string.Join(", ", list));
                }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            expectedOutput: IncludeExpectedOutput("goo"),
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
                // (7,19): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //         Goo([with(null), "goo"]);
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(7, 19));

        var compilation = verifier.Compilation;
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Last());

        var invocation = compilation.SyntaxTrees.Last().GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
        var symbolInfo = semanticModel.GetSymbolInfo(invocation);
        AssertEx.Equal("void Program.Goo<System.String!>(MyCollection<System.String!>! list)", symbolInfo.Symbol.ToTestDisplayString(true));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.String!>! MyBuilder.Create<System.String!>(System.String! value, System.ReadOnlySpan<System.String!> items)");
    }

    [Fact]
    public void CollectionBuilderNonNullParameterPassedNull_Inference2()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : IEnumerable<T>
            {
                private readonly List<T> _items;
                public MyCollection(T? value, ReadOnlySpan<T> items)
                {
                    _items = new();
                    _items.AddRange(items.ToArray());
                }
                public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>(T? value, ReadOnlySpan<T> items) => new(value, items);
            }
            """;
        string sourceB = """
            #nullable enable
            using System;
            class Program
            {
                static void Main()
                {
                    Goo([with(null), "goo"]);
                }

                static void Goo<T>(MyCollection<T> list)
                {
                    Console.WriteLine(string.Join(", ", list));
                }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            expectedOutput: IncludeExpectedOutput("goo"),
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics();

        var compilation = verifier.Compilation;
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Last());

        var invocation = compilation.SyntaxTrees.Last().GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
        var symbolInfo = semanticModel.GetSymbolInfo(invocation);
        AssertEx.Equal("void Program.Goo<System.String!>(MyCollection<System.String!>! list)", symbolInfo.Symbol.ToTestDisplayString(true));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.String!>! MyBuilder.Create<System.String!>(System.String? value, System.ReadOnlySpan<System.String!> items)");
    }

    [Fact]
    public void TestInferTypeFromOtherBranch1()
    {
        var source = """
            #nullable enable
            using System;
            using System.Collections.Generic;

            var v = true
                ? [with(null), "a", "b"]
                : new MyCollection<string>("");
            Console.WriteLine(string.Join(", ", v));

            class MyCollection<T> : List<T>
            {
                public MyCollection(T arg)
                {
                }
            }
            """;

        var verifier = CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("a, b")).VerifyDiagnostics(
            // (6,13): warning CS8625: Cannot convert null literal to non-nullable reference type.
            //     ? [with(null), "a", "b"]
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(6, 13));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.String!>.MyCollection(System.String! arg)");
    }

    [Fact]
    public void TestInferTypeFromOtherBranch2()
    {
        var source = """
            #nullable enable
            using System;
            using System.Collections.Generic;

            var v = true
                ? [with(""), null]
                : new MyCollection<string>("");
            Console.WriteLine(string.Join(", ", v));

            class MyCollection<T> : List<T>
            {
                public MyCollection(T arg)
                {
                }
            }
            """;

        var verifier = CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("")).VerifyDiagnostics(
                // (6,18): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     ? [with(""), null]
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(6, 18));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.String!>.MyCollection(System.String! arg)");
    }

    [Fact]
    public void TestTernaryInference1()
    {
        var source = """
            #nullable enable
            using System;
            using System.Collections.Generic;

            var v = true
                ? Goo([with(""), null])
                : Goo([with(""), ""]);
            Console.WriteLine(string.Join(", ", v));

            MyCollection<T> Goo<T>(MyCollection<T> c) { return c; }

            class MyCollection<T> : List<T>
            {
                public MyCollection(T arg)
                {
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (6,7): error CS0411: The type arguments for method 'Goo<T>(MyCollection<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
            //     ? Goo([with(""), null])
            Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Goo").WithArguments("Goo<T>(MyCollection<T>)").WithLocation(6, 7));
    }

    [Fact]
    public void TestTernaryInference2()
    {
        var source = """
            #nullable enable
            using System;
            using System.Collections.Generic;

            var v = true
                ? Goo([with(""), null])
                : Goo([with(null), ""]);
            Console.WriteLine(string.Join(", ", v));
            
            MyCollection<T> Goo<T>(MyCollection<T> c) { return c; }

            class MyCollection<T> : List<T>
            {
                public MyCollection(T arg)
                {
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (6,7): error CS0411: The type arguments for method 'Goo<T>(MyCollection<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
            //     ? Goo([with(""), null])
            Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Goo").WithArguments("Goo<T>(MyCollection<T>)").WithLocation(6, 7));
    }

    [Fact]
    public void TestTernaryInference3()
    {
        var source = """
            #nullable enable
            using System;
            using System.Collections.Generic;

            var v = true
                ? Goo([with(null), null, ""])
                : Goo([with(null), ""]);
            Console.WriteLine(string.Join(", ", v));
            
            MyCollection<T> Goo<T>(MyCollection<T> c) { return c; }

            class MyCollection<T> : List<T>
            {
                public MyCollection(T arg)
                {
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput(",")).VerifyDiagnostics(
            // (6,7): warning CS8619: Nullability of reference types in value of type 'MyCollection<string?>' doesn't match target type 'MyCollection<string>'.
            //     ? Goo([with(null), null, ""])
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, @"Goo([with(null), null, """"])").WithArguments("MyCollection<string?>", "MyCollection<string>").WithLocation(6, 7));
    }

    [Fact]
    public void TestTernaryInference4()
    {
        var source = """
            #nullable enable
            using System;
            using System.Collections.Generic;

            var v = true
                ? Goo([with(null), null, ""])
                : Goo([with(null), "", null]);
            Console.WriteLine(string.Join(", ", v));
            
            MyCollection<T> Goo<T>(MyCollection<T> c) { return c; }

            class MyCollection<T> : List<T>
            {
                public MyCollection(T arg)
                {
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput(",")).VerifyDiagnostics(
            // (6,7): warning CS8619: Nullability of reference types in value of type 'MyCollection<string?>' doesn't match target type 'MyCollection<string>'.
            //     ? Goo([with(null), null, ""])
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, @"Goo([with(null), null, """"])").WithArguments("MyCollection<string?>", "MyCollection<string>").WithLocation(6, 7));
    }

    [Fact]
    public void CollectionBuilderInferenceIncorrectArity()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
            }
            class MyBuilder
            {
                // This is incorrect as the arity doesn't match MyCollection<T>
                public static MyCollection<TElement> Create<TElement, TOther>(TOther arg, ReadOnlySpan<TElement> elements) => default!;
            }
            """;
        string sourceB = """
            #nullable enable
            class Program
            {
                static void Main()
                {
                    MyCollection<string> x = [with(""), "goo"];
                }
            }
            """;

        // PROTOTYPE: Need to update this diagnostic message to not state that it's a single ROS parameter.
        // But it needs to have a final ROS parameter.
        CreateCompilation([sourceA, sourceB], targetFramework: TargetFramework.Net80).VerifyDiagnostics(
            // (6,34): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         MyCollection<string> x = [with(""), "goo"];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, @"[with(""""), ""goo""]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(6, 34));
    }

    #region AllowNull

    [Fact]
    public void ConstructorNonNullableParameterWithAllowNull_Null()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList([AllowNull] string arg)
                {
                }
            }

            class C
            {
                static void Main()
                {
                    string? s = null;
                    MyList<int> list = [with(s), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (18,13): warning CS8604: Possible null reference argument for parameter 's' in 'void C.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void C.Goo(string s)").WithLocation(18, 13));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.String! arg)");
    }

    [Fact]
    public void ConstructorNonNullableParameterWithAllowNull_NotNull()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList([AllowNull] string arg)
                {
                }
            }

            class C
            {
                static void Main()
                {
                    string? s = "";
                    MyList<int> list = [with(s), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics();

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.String! arg)");
    }

    [Fact]
    public void ConstructorNullableParameterWithAllowNull_Null()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList([AllowNull] string? arg)
                {
                }
            }

            class C
            {
                static void Main()
                {
                    string? s = null;
                    MyList<int> list = [with(s), 1, 2];
                    Goo(s);
                }
            
                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (18,13): warning CS8604: Possible null reference argument for parameter 's' in 'void C.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void C.Goo(string s)").WithLocation(18, 13));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.String? arg)");
    }

    [Fact]
    public void ConstructorNullableParameterWithAllowNull_NotNull()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList([AllowNull] string? arg)
                {
                }
            }

            class C
            {
                static void Main()
                {
                    string? s = "";
                    MyList<int> list = [with(s), 1, 2];
                    Goo(s);
                }
            
                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics();

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.String? arg)");
    }

    [Fact]
    public void CollectionBuilderNonNullableParameterWithAllowNull_Null()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
                public MyCollection(string? value, ReadOnlySpan<T> items)
                {
                }
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>([AllowNull] string value, ReadOnlySpan<T> items) => new(value, items);
            }
            """;
        string sourceB = """
            #nullable enable
            class Program
            {
                static void Main()
                {
                    string? s = null;
                    MyCollection<int> c = [with(s), 1, 2];
                    Goo(s);
                }
            
                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (8,13): warning CS8604: Possible null reference argument for parameter 's' in 'void Program.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void Program.Goo(string s)").WithLocation(8, 13));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.String! value, System.ReadOnlySpan<System.Int32> items)");
    }

    [Fact]
    public void CollectionBuilderNonNullableParameterWithAllowNull_NotNull()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
                public MyCollection(string? value, ReadOnlySpan<T> items)
                {
                }
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>([AllowNull] string value, ReadOnlySpan<T> items) => new(value, items);
            }
            """;
        string sourceB = """
            #nullable enable
            class Program
            {
                static void Main()
                {
                    string? s = "";
                    MyCollection<int> c = [with(s), 1, 2];
                    Goo(s);
                }
            
                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics();

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.String! value, System.ReadOnlySpan<System.Int32> items)");
    }

    [Fact]
    public void CollectionBuilderNullableParameterWithAllowNull_Null()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
                public MyCollection(string? value, ReadOnlySpan<T> items)
                {
                }
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>([AllowNull] string? value, ReadOnlySpan<T> items) => new(value, items);
            }
            """;
        string sourceB = """
            #nullable enable
            
            class Program
            {
                static void Main()
                {
                    string? s = null;
                    MyCollection<int> c = [with(s), 1, 2];
                    Goo(s);
                }
            
                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (9,13): warning CS8604: Possible null reference argument for parameter 's' in 'void Program.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void Program.Goo(string s)").WithLocation(9, 13));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.String? value, System.ReadOnlySpan<System.Int32> items)");
    }

    [Fact]
    public void CollectionBuilderNullableParameterWithAllowNull_NotNull()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
                public MyCollection(string? value, ReadOnlySpan<T> items)
                {
                }
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>([AllowNull] string? value, ReadOnlySpan<T> items) => new(value, items);
            }
            """;
        string sourceB = """
            #nullable enable
            
            class Program
            {
                static void Main()
                {
                    string? s = "";
                    MyCollection<int> c = [with(s), 1, 2];
                    Goo(s);
                }
            
                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics();

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.String? value, System.ReadOnlySpan<System.Int32> items)");
    }

    #endregion

    #region DisallowNull

    [Fact]
    public void ConstructorNonNullableParameterWithDisallowNull_Null()
    {
        var source = """
            #nullable enable
            
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList([DisallowNull] string arg)
                {
                }
            }

            class C
            {
                static void Main()
                {
                    string? s = null;
                    MyList<int> list = [with(s), 1, 2];
                    Goo(s);
                }
            
                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (18,34): warning CS8604: Possible null reference argument for parameter 'arg' in 'MyList<int>.MyList(string arg)'.
            //         MyList<int> list = [with(s), 1, 2];
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("arg", "MyList<int>.MyList(string arg)").WithLocation(18, 34));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.String! arg)");
    }

    [Fact]
    public void ConstructorNonNullableParameterWithDisallowNull_NotNull()
    {
        var source = """
            #nullable enable
            
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList([DisallowNull] string arg)
                {
                }
            }

            class C
            {
                static void Main()
                {
                    string? s = "";
                    MyList<int> list = [with(s), 1, 2];
                    Goo(s);
                }
            
                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics();

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.String! arg)");
    }

    [Fact]
    public void ConstructorNullableParameterWithDisallowNull_Null()
    {
        var source = """
            #nullable enable
            
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList([DisallowNull] string? arg)
                {
                }
            }

            class C
            {
                static void Main()
                {
                    string? s = null;
                    MyList<int> list = [with(s), 1, 2];
                    Goo(s);
                }
            
                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (18,34): warning CS8604: Possible null reference argument for parameter 'arg' in 'MyList<int>.MyList(string? arg)'.
            //         MyList<int> list = [with(s), 1, 2];
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("arg", "MyList<int>.MyList(string? arg)").WithLocation(18, 34));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.String? arg)");
    }

    [Fact]
    public void ConstructorNullableParameterWithDisallowNull_NotNull()
    {
        var source = """
            #nullable enable
            
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList([DisallowNull] string? arg)
                {
                }
            }

            class C
            {
                static void Main()
                {
                    string? s = "";
                    MyList<int> list = [with(s), 1, 2];
                    Goo(s);
                }
            
                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics();

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.String? arg)");
    }

    [Fact]
    public void CollectionBuilderNonNullableParameterWithDisallowNull_Null()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
                public MyCollection(string? value, ReadOnlySpan<T> items)
                {
                }
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>([DisallowNull] string value, ReadOnlySpan<T> items) => new(value, items);
            }
            """;
        string sourceB = """
            #nullable enable
            
            class Program
            {
                static void Main()
                {
                    string? s = null;
                    MyCollection<int> c = [with(s), 1, 2];
                    Goo(s);
                }
            
                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (8,37): warning CS8604: Possible null reference argument for parameter 'value' in 'MyCollection<int> MyBuilder.Create<int>(string value, ReadOnlySpan<int> items)'.
            //         MyCollection<int> c = [with(s), 1, 2];
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("value", "MyCollection<int> MyBuilder.Create<int>(string value, ReadOnlySpan<int> items)").WithLocation(8, 37));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.String! value, System.ReadOnlySpan<System.Int32> items)");
    }

    [Fact]
    public void CollectionBuilderNonNullableParameterWithDisallowNull_NotNull()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
                public MyCollection(string? value, ReadOnlySpan<T> items)
                {
                }
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>([DisallowNull] string value, ReadOnlySpan<T> items) => new(value, items);
            }
            """;
        string sourceB = """
            #nullable enable
            
            class Program
            {
                static void Main()
                {
                    string? s = "";
                    MyCollection<int> c = [with(s), 1, 2];
                    Goo(s);
                }
            
                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics();

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.String! value, System.ReadOnlySpan<System.Int32> items)");
    }

    [Fact]
    public void CollectionBuilderNullableParameterWithDisallowNull_Null()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
                public MyCollection(string? value, ReadOnlySpan<T> items)
                {
                }
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>([DisallowNull] string? value, ReadOnlySpan<T> items) => new(value, items);
            }
            """;
        string sourceB = """
            #nullable enable
            
            class Program
            {
                static void Main()
                {
                    string? s = null;
                    MyCollection<int> c = [with(s), 1, 2];
                    Goo(s);
                }
            
                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (8,37): warning CS8604: Possible null reference argument for parameter 'value' in 'MyCollection<int> MyBuilder.Create<int>(string? value, ReadOnlySpan<int> items)'.
            //         MyCollection<int> c = [with(s), 1, 2];
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("value", "MyCollection<int> MyBuilder.Create<int>(string? value, ReadOnlySpan<int> items)").WithLocation(8, 37));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.String? value, System.ReadOnlySpan<System.Int32> items)");
    }

    [Fact]
    public void CollectionBuilderNullableParameterWithDisallowNull_NotNull()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
                public MyCollection(string? value, ReadOnlySpan<T> items)
                {
                }
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>([DisallowNull] string? value, ReadOnlySpan<T> items) => new(value, items);
            }
            """;
        string sourceB = """
            #nullable enable
            
            class Program
            {
                static void Main()
                {
                    string? s = "";
                    MyCollection<int> c = [with(s), 1, 2];
                    Goo(s);
                }
            
                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics();

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.String? value, System.ReadOnlySpan<System.Int32> items)");
    }

    #endregion

    #region MaybeNull

    [Fact]
    public void ConstructorNonNullableParameterWithMaybeNull_Null()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList([MaybeNull] string arg)
                {
                }
            }

            class C
            {
                static void Main()
                {
                    string? s = null;
                    MyList<int> list = [with(s), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (17,34): warning CS8604: Possible null reference argument for parameter 'arg' in 'MyList<int>.MyList(string arg)'.
            //         MyList<int> list = [with(s), 1, 2];
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("arg", "MyList<int>.MyList(string arg)").WithLocation(17, 34),
            // (18,13): warning CS8604: Possible null reference argument for parameter 's' in 'void C.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void C.Goo(string s)").WithLocation(18, 13));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.String! arg)");
    }

    [Fact]
    public void ConstructorNonNullableParameterWithMaybeNull_NotNull()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList([MaybeNull] string arg)
                {
                }
            }

            class C
            {
                static void Main()
                {
                    string? s = "";
                    MyList<int> list = [with(s), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (18,13): warning CS8604: Possible null reference argument for parameter 's' in 'void C.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void C.Goo(string s)").WithLocation(18, 13));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.String! arg)");
    }

    [Fact]
    public void ConstructorNullableParameterWithMaybeNull_Null()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList([MaybeNull] string? arg)
                {
                }
            }

            class C
            {
                static void Main()
                {
                    string? s = null;
                    MyList<int> list = [with(s), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (18,13): warning CS8604: Possible null reference argument for parameter 's' in 'void C.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void C.Goo(string s)").WithLocation(18, 13));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.String? arg)");
    }

    [Fact]
    public void ConstructorNullableParameterWithMaybeNull_NotNull()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList([MaybeNull] string? arg)
                {
                }
            }

            class C
            {
                static void Main()
                {
                    string? s = "";
                    MyList<int> list = [with(s), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (18,13): warning CS8604: Possible null reference argument for parameter 's' in 'void C.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void C.Goo(string s)").WithLocation(18, 13));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.String? arg)");
    }

    [Fact]
    public void CollectionBuilderNonNullableParameterWithMaybeNull_Null()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
                public MyCollection(string? value, ReadOnlySpan<T> items)
                {
                }
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>([MaybeNull] string value, ReadOnlySpan<T> items) => new(value, items);
            }
            """;
        string sourceB = """
            #nullable enable
            class Program
            {
                static void Main()
                {
                    string? s = null;
                    MyCollection<int> c = [with(s), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (7,37): warning CS8604: Possible null reference argument for parameter 'value' in 'MyCollection<int> MyBuilder.Create<int>(string value, ReadOnlySpan<int> items)'.
            //         MyCollection<int> c = [with(s), 1, 2];
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("value", "MyCollection<int> MyBuilder.Create<int>(string value, ReadOnlySpan<int> items)").WithLocation(7, 37),
            // (8,13): warning CS8604: Possible null reference argument for parameter 's' in 'void Program.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void Program.Goo(string s)").WithLocation(8, 13));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.String! value, System.ReadOnlySpan<System.Int32> items)");
    }

    [Fact]
    public void CollectionBuilderNonNullableParameterWithMaybeNull_NotNull()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
                public MyCollection(string? value, ReadOnlySpan<T> items)
                {
                }
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>([MaybeNull] string value, ReadOnlySpan<T> items) => new(value, items);
            }
            """;
        string sourceB = """
            #nullable enable
            class Program
            {
                static void Main()
                {
                    string? s = "";
                    MyCollection<int> c = [with(s), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (8,13): warning CS8604: Possible null reference argument for parameter 's' in 'void Program.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void Program.Goo(string s)").WithLocation(8, 13));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.String! value, System.ReadOnlySpan<System.Int32> items)");
    }

    [Fact]
    public void CollectionBuilderNullableParameterWithMaybeNull_Null()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
                public MyCollection(string? value, ReadOnlySpan<T> items)
                {
                }
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>([MaybeNull] string? value, ReadOnlySpan<T> items) => new(value, items);
            }
            """;
        string sourceB = """
            #nullable enable
            class Program
            {
                static void Main()
                {
                    string? s = null;
                    MyCollection<int> c = [with(s), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (8,13): warning CS8604: Possible null reference argument for parameter 's' in 'void Program.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void Program.Goo(string s)").WithLocation(8, 13));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.String? value, System.ReadOnlySpan<System.Int32> items)");
    }

    [Fact]
    public void CollectionBuilderNullableParameterWithMaybeNull_NotNull()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
                public MyCollection(string? value, ReadOnlySpan<T> items)
                {
                }
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>([MaybeNull] string? value, ReadOnlySpan<T> items) => new(value, items);
            }
            """;
        string sourceB = """
            #nullable enable
            class Program
            {
                static void Main()
                {
                    string? s = "";
                    MyCollection<int> c = [with(s), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (8,13): warning CS8604: Possible null reference argument for parameter 's' in 'void Program.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void Program.Goo(string s)").WithLocation(8, 13));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.String? value, System.ReadOnlySpan<System.Int32> items)");
    }

    #endregion

    #region NotNull

    [Fact]
    public void ConstructorNullableParameterWithNotNull_Null()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList([NotNull] string? arg)
                {
                    arg = "";
                }
            }

            class C
            {
                static void Main()
                {
                    string? s = null;
                    MyList<int> list = [with(s), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics();

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.String? arg)");
    }

    [Fact]
    public void ConstructorNullableParameterWithNotNull_NotNull()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList([NotNull] string? arg)
                {
                    arg = "";
                }
            }

            class C
            {
                static void Main()
                {
                    string? s = "";
                    MyList<int> list = [with(s), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics();

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.String? arg)");
    }

    [Fact]
    public void ConstructorNonNullableParameterWithNotNull_Null()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList([NotNull] string arg)
                {
                    arg = "";
                }
            }

            class C
            {
                static void Main()
                {
                    string? s = null;
                    MyList<int> list = [with(s), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (18,34): warning CS8604: Possible null reference argument for parameter 'arg' in 'MyList<int>.MyList(string arg)'.
            //         MyList<int> list = [with(s), 1, 2];
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("arg", "MyList<int>.MyList(string arg)").WithLocation(18, 34));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.String! arg)");
    }

    [Fact]
    public void ConstructorNonNullableParameterWithNotNull_NotNull()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList([NotNull] string arg)
                {
                    arg = "";
                }
            }

            class C
            {
                static void Main()
                {
                    string? s = "";
                    MyList<int> list = [with(s), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics();

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.String! arg)");
    }

    [Fact]
    public void CollectionBuilderNullableParameterWithNotNull_Null()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
                public MyCollection(string? value, ReadOnlySpan<T> items)
                {
                }
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>([NotNull] string? value, ReadOnlySpan<T> items)
                {
                    value = "";
                    return new(value, items);
                }
            }
            """;
        string sourceB = """
            #nullable enable
            class Program
            {
                static void Main()
                {
                    string? s = null;
                    MyCollection<int> c = [with(s), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics();

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.String? value, System.ReadOnlySpan<System.Int32> items)");
    }

    [Fact]
    public void CollectionBuilderNullableParameterWithNotNull_NotNull()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
                public MyCollection(string? value, ReadOnlySpan<T> items)
                {
                }
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>([NotNull] string? value, ReadOnlySpan<T> items)
                {
                    value = "";
                    return new(value, items);
                }
            }
            """;
        string sourceB = """
            #nullable enable
            class Program
            {
                static void Main()
                {
                    string? s = "";
                    MyCollection<int> c = [with(s), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics();

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.String? value, System.ReadOnlySpan<System.Int32> items)");
    }

    [Fact]
    public void CollectionBuilderNonNullableParameterWithNotNull_Null()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
                public MyCollection(string? value, ReadOnlySpan<T> items)
                {
                }
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>([NotNull] string value, ReadOnlySpan<T> items)
                {
                    value = "";
                    return new(value, items);
                }
            }
            """;
        string sourceB = """
            #nullable enable
            class Program
            {
                static void Main()
                {
                    string? s = null;
                    MyCollection<int> c = [with(s), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (7,37): warning CS8604: Possible null reference argument for parameter 'value' in 'MyCollection<int> MyBuilder.Create<int>(string value, ReadOnlySpan<int> items)'.
            //         MyCollection<int> c = [with(s), 1, 2];
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("value", "MyCollection<int> MyBuilder.Create<int>(string value, ReadOnlySpan<int> items)").WithLocation(7, 37));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.String! value, System.ReadOnlySpan<System.Int32> items)");
    }

    [Fact]
    public void CollectionBuilderNonNullableParameterWithNotNull_NotNull()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
                public MyCollection(string? value, ReadOnlySpan<T> items)
                {
                }
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>([NotNull] string value, ReadOnlySpan<T> items)
                {
                    value = "";
                    return new(value, items);
                }
            }
            """;
        string sourceB = """
            #nullable enable
            class Program
            {
                static void Main()
                {
                    string? s = "";
                    MyCollection<int> c = [with(s), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics();

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.String! value, System.ReadOnlySpan<System.Int32> items)");
    }

    #endregion

    #region NotNullIfNotNull

    [Fact]
    public void ConstructorParameterWithNotNullIfNotNull_BothNull()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList([NotNullIfNotNull(nameof(other))] string? arg, string? other)
                {
                }
            }

            class C
            {
                static void Main()
                {
                    string? arg = null;
                    string? other = null;
                    MyList<int> list = [with(arg, other), 1, 2];
                    Goo(arg);
                    Goo(other);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (19,13): warning CS8604: Possible null reference argument for parameter 's' in 'void C.Goo(string s)'.
            //         Goo(arg);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "arg").WithArguments("s", "void C.Goo(string s)").WithLocation(19, 13),
            // (20,13): warning CS8604: Possible null reference argument for parameter 's' in 'void C.Goo(string s)'.
            //         Goo(other);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "other").WithArguments("s", "void C.Goo(string s)").WithLocation(20, 13));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.String? arg, System.String? other)");
    }

    [Fact]
    public void ConstructorParameterWithNotNullIfNotNull_ArgNull_OtherNotNull()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList([NotNullIfNotNull(nameof(other))] string? arg, string? other)
                {
                }
            }

            class C
            {
                static void Main()
                {
                    string? arg = null;
                    string other = "";
                    MyList<int> list = [with(arg, other), 1, 2];
                    Goo(arg);
                    Goo(other);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (19,13): warning CS8604: Possible null reference argument for parameter 's' in 'void C.Goo(string s)'.
            //         Goo(arg);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "arg").WithArguments("s", "void C.Goo(string s)").WithLocation(19, 13));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.String? arg, System.String? other)");
    }

    [Fact]
    public void ConstructorParameterWithNotNullIfNotNull_ArgNotNull_OtherNull()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList([NotNullIfNotNull(nameof(other))] string? arg, string? other)
                {
                }
            }

            class C
            {
                static void Main()
                {
                    string? arg = "";
                    string? other = null;
                    MyList<int> list = [with(arg, other), 1, 2];
                    Goo(arg);
                    Goo(other);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (20,13): warning CS8604: Possible null reference argument for parameter 's' in 'void C.Goo(string s)'.
            //         Goo(other);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "other").WithArguments("s", "void C.Goo(string s)").WithLocation(20, 13));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.String? arg, System.String? other)");
    }

    [Fact]
    public void ConstructorParameterWithNotNullIfNotNull_BothNotNull()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList([NotNullIfNotNull(nameof(other))] string? arg, string? other)
                {
                }
            }

            class C
            {
                static void Main()
                {
                    string? arg = "";
                    string? other = "";
                    MyList<int> list = [with(arg, other), 1, 2];
                    Goo(arg);
                    Goo(other);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics();

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.String? arg, System.String? other)");
    }

    [Fact]
    public void CollectionBuilderParameterWithNotNullIfNotNull_BothNull()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
                public MyCollection(string? value, string? other, ReadOnlySpan<T> items)
                {
                }
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>([NotNullIfNotNull(nameof(other))] string? value, string? other, ReadOnlySpan<T> items)
                    => new(value, other, items);
            }
            """;
        string sourceB = """
            #nullable enable
            class Program
            {
                static void Main()
                {
                    string? arg = null;
                    string? other = null;
                    MyCollection<int> c = [with(arg, other), 1, 2];
                    Goo(arg);
                    Goo(other);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (9,13): warning CS8604: Possible null reference argument for parameter 's' in 'void Program.Goo(string s)'.
            //         Goo(arg);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "arg").WithArguments("s", "void Program.Goo(string s)").WithLocation(9, 13),
            // (10,13): warning CS8604: Possible null reference argument for parameter 's' in 'void Program.Goo(string s)'.
            //         Goo(other);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "other").WithArguments("s", "void Program.Goo(string s)").WithLocation(10, 13));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.String? value, System.String? other, System.ReadOnlySpan<System.Int32> items)");
    }

    [Fact]
    public void CollectionBuilderParameterWithNotNullIfNotNull_ArgNull_OtherNotNull()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
                public MyCollection(string? value, string? other, ReadOnlySpan<T> items)
                {
                }
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>([NotNullIfNotNull(nameof(other))] string? value, string? other, ReadOnlySpan<T> items)
                    => new(value, other, items);
            }
            """;
        string sourceB = """
            #nullable enable
            class Program
            {
                static void Main()
                {
                    string? arg = null;
                    string? other = "";
                    MyCollection<int> c = [with(arg, other), 1, 2];
                    Goo(arg);
                    Goo(other);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (9,13): warning CS8604: Possible null reference argument for parameter 's' in 'void Program.Goo(string s)'.
            //         Goo(arg);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "arg").WithArguments("s", "void Program.Goo(string s)").WithLocation(9, 13));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.String? value, System.String? other, System.ReadOnlySpan<System.Int32> items)");
    }

    [Fact]
    public void CollectionBuilderParameterWithNotNullIfNotNull_ArgNotNull_OtherNull()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
                public MyCollection(string? value, string? other, ReadOnlySpan<T> items)
                {
                }
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>([NotNullIfNotNull(nameof(other))] string? value, string? other, ReadOnlySpan<T> items)
                    => new(value, other, items);
            }
            """;
        string sourceB = """
            #nullable enable
            class Program
            {
                static void Main()
                {
                    string? arg = "";
                    string? other = null;
                    MyCollection<int> c = [with(arg, other), 1, 2];
                    Goo(arg);
                    Goo(other);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
                // (10,13): warning CS8604: Possible null reference argument for parameter 's' in 'void Program.Goo(string s)'.
                //         Goo(other);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "other").WithArguments("s", "void Program.Goo(string s)").WithLocation(10, 13));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.String? value, System.String? other, System.ReadOnlySpan<System.Int32> items)");
    }

    [Fact]
    public void CollectionBuilderParameterWithNotNullIfNotNull_BothNotNull()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
                public MyCollection(string? value, string? other, ReadOnlySpan<T> items)
                {
                }
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>([NotNullIfNotNull(nameof(other))] string? value, string? other, ReadOnlySpan<T> items)
                    => new(value, other, items);
            }
            """;
        string sourceB = """
            #nullable enable
            class Program
            {
                static void Main()
                {
                    string? arg = "";
                    string? other = "";
                    MyCollection<int> c = [with(arg, other), 1, 2];
                    Goo(arg);
                    Goo(other);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics();

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.String? value, System.String? other, System.ReadOnlySpan<System.Int32> items)");
    }

    #endregion

    #region DoesNotReturnIf

    [Fact]
    public void ConstructorWithDoesNotReturnIf_Null_NullCheck()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList([DoesNotReturnIf(true)] bool b)
                {
                }
            }

            class C
            {
                static void Main()
                {
                    string? s = null;
                    MyList<int> list = [with(s == null), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics();

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.Boolean b)");
    }

    [Fact]
    public void ConstructorWithDoesNotReturnIf_Null_NotNullCheck()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList([DoesNotReturnIf(true)] bool b)
                {
                }
            }

            class C
            {
                static void Main()
                {
                    string? s = null;
                    MyList<int> list = [with(s != null), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (18,13): warning CS8604: Possible null reference argument for parameter 's' in 'void C.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void C.Goo(string s)").WithLocation(18, 13));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.Boolean b)");
    }

    [Fact]
    public void ConstructorWithDoesNotReturnIf_NotNull_NullCheck()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList([DoesNotReturnIf(true)] bool b)
                {
                }
            }

            class C
            {
                static void Main()
                {
                    string? s = "";
                    MyList<int> list = [with(s == null), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics();

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.Boolean b)");
    }

    [Fact]
    public void ConstructorWithDoesNotReturnIf_NotNull_NotNullCheck()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList([DoesNotReturnIf(true)] bool b)
                {
                }
            }

            class C
            {
                static void Main()
                {
                    string? s = "";
                    MyList<int> list = [with(s != null), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (18,13): warning CS8604: Possible null reference argument for parameter 's' in 'void C.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void C.Goo(string s)").WithLocation(18, 13));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyList<System.Int32>.MyList(System.Boolean b)");
    }

    [Fact]
    public void CollectionBuilderWithDoesNotReturnIf_Null_NullCheck()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>([DoesNotReturnIf(true)] bool value, ReadOnlySpan<T> items) => null!;
            }
            """;
        string sourceB = """
            #nullable enable
            class Program
            {
                static void Main()
                {
                    string? s = null;
                    MyCollection<int> c = [with(s == null), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics();

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.Boolean value, System.ReadOnlySpan<System.Int32> items)");
    }

    [Fact]
    public void CollectionBuilderWithDoesNotReturnIf_Null_NotNullCheck()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>([DoesNotReturnIf(true)] bool value, ReadOnlySpan<T> items) => null!;
            }
            """;
        string sourceB = """
            #nullable enable
            class Program
            {
                static void Main()
                {
                    string? s = null;
                    MyCollection<int> c = [with(s != null), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (8,13): warning CS8604: Possible null reference argument for parameter 's' in 'void Program.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void Program.Goo(string s)").WithLocation(8, 13));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.Boolean value, System.ReadOnlySpan<System.Int32> items)");
    }

    [Fact]
    public void CollectionBuilderWithDoesNotReturnIf_NotNull_NullCheck()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>([DoesNotReturnIf(true)] bool value, ReadOnlySpan<T> items) => null!;
            }
            """;
        string sourceB = """
            #nullable enable
            class Program
            {
                static void Main()
                {
                    string? s = "";
                    MyCollection<int> c = [with(s == null), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics();

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.Boolean value, System.ReadOnlySpan<System.Int32> items)");
    }

    [Fact]
    public void CollectionBuilderWithDoesNotReturnIf_NotNull_NotNullCheck()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>([DoesNotReturnIf(true)] bool value, ReadOnlySpan<T> items) => null!;
            }
            """;
        string sourceB = """
            #nullable enable
            class Program
            {
                static void Main()
                {
                    string? s = "";
                    MyCollection<int> c = [with(s != null), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (8,13): warning CS8604: Possible null reference argument for parameter 's' in 'void Program.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void Program.Goo(string s)").WithLocation(8, 13));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.Boolean value, System.ReadOnlySpan<System.Int32> items)");
    }

    #endregion

    #region DoesNotReturn

    [Fact]
    public void CollectionBuilderWithDoesNotReturn_NoArgs_NoWithElement()
    {
        var source = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;
            
            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
            }

            class MyBuilder
            {
                [DoesNotReturn]
                public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => throw null!;
            }

            class C
            {
                static void Main()
                {
                    string? s = null;
                    MyCollection<int> list = [];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics();
    }

    [Fact]
    public void CollectionBuilderWithDoesNotReturn_NoArgs_WithElement()
    {
        var source = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;
            
            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
            }
            
            class MyBuilder
            {
                [DoesNotReturn]
                public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => throw null!;
            }

            class C
            {
                static void Main()
                {
                    string? s = null;
                    MyCollection<int> list = [with()];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics();

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.ReadOnlySpan<System.Int32> items)");
    }

    [Fact]
    public void CollectionBuilderWithDoesNotReturn_Args_WithElement()
    {
        var source = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;
            
            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
            }
            
            class MyBuilder
            {
                [DoesNotReturn]
                public static MyCollection<T> Create<T>(bool b, ReadOnlySpan<T> items) => throw null!;
            }

            class C
            {
                static void Main()
                {
                    string? s = null;
                    MyCollection<int> list = [with(true)];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics();

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.Boolean b, System.ReadOnlySpan<System.Int32> items)");
    }

    #endregion

    #region MemberNotNull

    [Fact]
    public void CollectionBuilderWithMemberNotNull()
    {
        var source = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;
            
            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : List<T>
            {
            }
            
            static class MyBuilder
            {
                public static string? Singleton;

                [MemberNotNull(nameof(Singleton))]
                public static MyCollection<T> Create<T>(bool b, ReadOnlySpan<T> items) => throw null!;
            }

            static class Other
            {
                public static string? Singleton;

                [MemberNotNull(nameof(Singleton))]
                public static void EnsureNotNull() => throw null!;
            }

            class C
            {
                static void Main()
                {
                    Other.EnsureNotNull();
                    Goo(Other.Singleton);

                    MyCollection<int> list = [with(true)];
                    Goo(MyBuilder.Singleton);
                }

                static void Goo(string s) { }
            }
            """;

        // PROTOTYPE: MemberNotNull analysis does not flow across calls to collection builders yet. This is probably
        // because we lack a way to track that the collection builder affects 'MyBuilder' state, and thus the member
        // access to MyBuilder.Singleton.  This is unlike the normal static call case on 'Other' where both the method
        // call and the member access are off of the same 'Other' type in the code directly.
        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (14,27): warning CS0649: Field 'MyBuilder.Singleton' is never assigned to, and will always have its default value null
            //     public static string? Singleton;
            Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Singleton").WithArguments("MyBuilder.Singleton", "null").WithLocation(14, 27),
            // (22,27): warning CS0649: Field 'Other.Singleton' is never assigned to, and will always have its default value null
            //     public static string? Singleton;
            Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Singleton").WithArguments("Other.Singleton", "null").WithLocation(22, 27),
            // (36,13): warning CS8604: Possible null reference argument for parameter 's' in 'void C.Goo(string s)'.
            //         Goo(MyBuilder.Singleton);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "MyBuilder.Singleton").WithArguments("s", "void C.Goo(string s)").WithLocation(36, 13));

        AssertExpectedWithElementSymbol(verifier.Compilation, "MyCollection<System.Int32>! MyBuilder.Create<System.Int32>(System.Boolean b, System.ReadOnlySpan<System.Int32> items)");
    }

    #endregion
}
