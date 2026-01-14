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

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("2")).VerifyDiagnostics(
            // (16,34): warning CS8625: Cannot convert null literal to non-nullable reference type.
            //         MyList<int> list = [with(null), 1, 2];
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(16, 34));
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

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("2")).VerifyDiagnostics(
            // (17,34): warning CS8604: Possible null reference argument for parameter 'arg' in 'MyList<int>.MyList(string arg)'.
            //         MyList<int> list = [with(s), 1, 2];
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("arg", "MyList<int>.MyList(string arg)").WithLocation(17, 34));
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

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("2")).VerifyDiagnostics();
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

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("2")).VerifyDiagnostics(
            // (16,17): warning CS0219: The variable 's' is assigned but its value is never used
            //         string? s = null;
            Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s").WithArguments("s").WithLocation(16, 17));
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

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("1")).VerifyDiagnostics(
            // (16,19): warning CS8625: Cannot convert null literal to non-nullable reference type.
            //         Goo([with(null), ""]);
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(16, 19));
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
    }

    [Fact]
    public void ConstructorNullParameterNotNullAttribute1()
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

        CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics();
    }

    [Fact]
    public void ConstructorNotNullParameterMaybeNullAttribute1()
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

        CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (18,13): warning CS8604: Possible null reference argument for parameter 's' in 'void C.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void C.Goo(string s)").WithLocation(18, 13));
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

        CompileAndVerify(
            [sourceA, sourceB],
            expectedOutput: IncludeExpectedOutput("1, 2"),
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (7,37): warning CS8625: Cannot convert null literal to non-nullable reference type.
            //         MyCollection<int> c = [with(null), 1, 2];
            Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(7, 37));
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

        CompileAndVerify(
            [sourceA, sourceB],
            expectedOutput: IncludeExpectedOutput("1, 2"),
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (8,37): warning CS8604: Possible null reference argument for parameter 'value' in 'MyCollection<int> MyBuilder.Create<int>(string value, ReadOnlySpan<int> items)'.
            //         MyCollection<int> c = [with(s), 1, 2];
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("value", "MyCollection<int> MyBuilder.Create<int>(string value, ReadOnlySpan<int> items)").WithLocation(8, 37));
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

        CompileAndVerify(
            [sourceA, sourceB],
            expectedOutput: IncludeExpectedOutput("1, 2"),
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics();
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

        CompileAndVerify(
            [sourceA, sourceB],
            expectedOutput: IncludeExpectedOutput("1, 2"),
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (7,17): warning CS0219: The variable 's' is assigned but its value is never used
            //         string? s = null;
            Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s").WithArguments("s").WithLocation(7, 17));
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
        AssertEx.Equal("void Program.Goo<System.String!>(MyCollection<System.String!>!" +
            " list)", symbolInfo.Symbol.ToTestDisplayString(true));
    }

    [Fact]
    public void CollectionBuilderNullParameterNotNullAttribute()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : IEnumerable<T>
            {
                private readonly List<T> _items;
                public MyCollection(string? value, ReadOnlySpan<T> items)
                {
                    _items = new();
                    _items.AddRange(items.ToArray());
                }
                public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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

        CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics();
    }

    [Fact]
    public void CollectionBuilderNotNullParameterMaybeNullAttribute()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
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

        CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
                // (8,13): warning CS8604: Possible null reference argument for parameter 's' in 'void Program.Goo(string s)'.
                //         Goo(s);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void Program.Goo(string s)").WithLocation(8, 13));
    }

    [Fact]
    public void ConstructorNonNullParameterWithAllowNull()
    {
        var source = """
            #nullable enable
            using System;
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
                    Console.WriteLine($"{list.Count}");
                }
            }
            """;

        CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics();
    }

    [Fact]
    public void ConstructorNonNullParameterWithoutAllowNull()
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

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("2")).VerifyDiagnostics(
            // (17,34): warning CS8604: Possible null reference argument for parameter 'arg' in 'MyList<int>.MyList(string arg)'.
            //         MyList<int> list = [with(s), 1, 2];
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("arg", "MyList<int>.MyList(string arg)").WithLocation(17, 34));
    }

    [Fact]
    public void ConstructorNullableParameterWithDisallowNull()
    {
        var source = """
            #nullable enable
            using System;
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
                    Console.WriteLine($"{list.Count}");
                }
            }
            """;

        CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (18,34): warning CS8604: Possible null reference argument for parameter 'arg' in 'MyList<int>.MyList(string? arg)'.
            //         MyList<int> list = [with(s), 1, 2];
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("arg", "MyList<int>.MyList(string? arg)").WithLocation(18, 34));
    }

    [Fact]
    public void ConstructorNullableParameterWithoutDisallowNull()
    {
        var source = """
            #nullable enable
            using System;
            using System.Collections.Generic;

            class MyList<T> : List<T>
            {
                public MyList(string? arg)
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

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("2")).VerifyDiagnostics();
    }

    [Fact]
    public void CollectionBuilderNonNullParameterWithAllowNull()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : IEnumerable<T>
            {
                private readonly List<T> _items;
                public MyCollection(string? value, ReadOnlySpan<T> items)
                {
                    _items = new();
                    _items.AddRange(items.ToArray());
                }
                public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>([AllowNull] string value, ReadOnlySpan<T> items) => new(value, items);
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

        CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics();
    }

    [Fact]
    public void CollectionBuilderNonNullParameterWithoutAllowNull()
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
                public MyCollection(string? value, ReadOnlySpan<T> items)
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

        CompileAndVerify(
            [sourceA, sourceB],
            expectedOutput: IncludeExpectedOutput("1, 2"),
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (8,37): warning CS8604: Possible null reference argument for parameter 'value' in 'MyCollection<int> MyBuilder.Create<int>(string value, ReadOnlySpan<int> items)'.
            //         MyCollection<int> c = [with(s), 1, 2];
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("value", "MyCollection<int> MyBuilder.Create<int>(string value, ReadOnlySpan<int> items)").WithLocation(8, 37));
    }

    [Fact]
    public void CollectionBuilderNullableParameterWithDisallowNull()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : IEnumerable<T>
            {
                private readonly List<T> _items;
                public MyCollection(string? value, ReadOnlySpan<T> items)
                {
                    _items = new();
                    _items.AddRange(items.ToArray());
                }
                public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>([DisallowNull] string? value, ReadOnlySpan<T> items) => new(value, items);
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

        CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (8,37): warning CS8604: Possible null reference argument for parameter 'value' in 'MyCollection<int> MyBuilder.Create<int>(string? value, ReadOnlySpan<int> items)'.
            //         MyCollection<int> c = [with(s), 1, 2];
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("value", "MyCollection<int> MyBuilder.Create<int>(string? value, ReadOnlySpan<int> items)").WithLocation(8, 37));
    }

    [Fact]
    public void CollectionBuilderNullableParameterWithoutDisallowNull()
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
                public MyCollection(string? value, ReadOnlySpan<T> items)
                {
                    _items = new();
                    _items.AddRange(items.ToArray());
                }
                public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>(string? value, ReadOnlySpan<T> items) => new(value, items);
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

        CompileAndVerify(
            [sourceA, sourceB],
            expectedOutput: IncludeExpectedOutput("1, 2"),
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics();
    }

    [Fact]
    public void ConstructorNullParameterWithNotNull()
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

        CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics();
    }

    [Fact]
    public void ConstructorNullParameterWithoutNotNull()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;

            class MyList<T> : List<T>
            {
                public MyList(string? arg)
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

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("")).VerifyDiagnostics(
            // (17,13): warning CS8604: Possible null reference argument for parameter 's' in 'void C.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void C.Goo(string s)").WithLocation(17, 13));
    }

    [Fact]
    public void ConstructorNonNullParameterWithMaybeNull()
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
                    string s = "";
                    MyList<int> list = [with(s), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (18,13): warning CS8604: Possible null reference argument for parameter 's' in 'void C.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void C.Goo(string s)").WithLocation(18, 13));
    }

    [Fact]
    public void ConstructorNonNullParameterWithoutMaybeNull()
    {
        var source = """
            #nullable enable
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
                    string s = "";
                    MyList<int> list = [with(s), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("")).VerifyDiagnostics();
    }

    [Fact]
    public void CollectionBuilderNullParameterWithNotNull()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : IEnumerable<T>
            {
                private readonly List<T> _items;
                public MyCollection(string? value, ReadOnlySpan<T> items)
                {
                    _items = new();
                    _items.AddRange(items.ToArray());
                }
                public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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

        CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics();
    }

    [Fact]
    public void CollectionBuilderNullParameterWithoutNotNull()
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
                public MyCollection(string? value, ReadOnlySpan<T> items)
                {
                    _items = new();
                    _items.AddRange(items.ToArray());
                }
                public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>(string? value, ReadOnlySpan<T> items) => new(value, items);
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

        CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (8,13): warning CS8604: Possible null reference argument for parameter 's' in 'void Program.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void Program.Goo(string s)").WithLocation(8, 13));
    }

    [Fact]
    public void CollectionBuilderNonNullParameterWithMaybeNull()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
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
                public static MyCollection<T> Create<T>([MaybeNull] string value, ReadOnlySpan<T> items) => new(value, items);
            }
            """;
        string sourceB = """
            #nullable enable
            class Program
            {
                static void Main()
                {
                    string s = "";
                    MyCollection<int> c = [with(s), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (8,13): warning CS8604: Possible null reference argument for parameter 's' in 'void Program.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void Program.Goo(string s)").WithLocation(8, 13));
    }

    [Fact]
    public void CollectionBuilderNonNullParameterWithoutMaybeNull()
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
            class Program
            {
                static void Main()
                {
                    string s = "";
                    MyCollection<int> c = [with(s), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        CompileAndVerify(
            [sourceA, sourceB],
            expectedOutput: IncludeExpectedOutput(""),
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics();
    }

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
                    string? s = null;
                    string? other = null;
                    MyList<int> list = [with(s, other), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (19,13): warning CS8604: Possible null reference argument for parameter 's' in 'void C.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void C.Goo(string s)").WithLocation(19, 13));
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
                    string? s = null;
                    string other = "";
                    MyList<int> list = [with(s, other), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (19,13): warning CS8604: Possible null reference argument for parameter 's' in 'void C.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void C.Goo(string s)").WithLocation(19, 13));
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
                    string? s = "";
                    string? other = null;
                    MyList<int> list = [with(s, other), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (19,13): warning CS8604: Possible null reference argument for parameter 's' in 'void C.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void C.Goo(string s)").WithLocation(19, 13));
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
                    string? s = "";
                    string? other = "";
                    MyList<int> list = [with(s, other), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (19,13): warning CS8604: Possible null reference argument for parameter 's' in 'void C.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void C.Goo(string s)").WithLocation(19, 13));
    }

    [Fact]
    public void ConstructorParameterWithoutNotNullIfNotNull_BothNull()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList(string? arg, string? other)
                {
                }
            }

            class C
            {
                static void Main()
                {
                    string? s = null;
                    string? other = null;
                    MyList<int> list = [with(s, other), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (19,13): warning CS8604: Possible null reference argument for parameter 's' in 'void C.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void C.Goo(string s)").WithLocation(19, 13));
    }

    [Fact]
    public void ConstructorParameterWithoutNotNullIfNotNull_ArgNull_OtherNotNull()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList(string? arg, string? other)
                {
                }
            }

            class C
            {
                static void Main()
                {
                    string? s = null;
                    string other = "";
                    MyList<int> list = [with(s, other), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (19,13): warning CS8604: Possible null reference argument for parameter 's' in 'void C.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void C.Goo(string s)").WithLocation(19, 13));
    }

    [Fact]
    public void ConstructorParameterWithoutNotNullIfNotNull_ArgNotNull_OtherNull()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList(string? arg, string? other)
                {
                }
            }

            class C
            {
                static void Main()
                {
                    string? s = "";
                    string? other = null;
                    MyList<int> list = [with(s, other), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (19,13): warning CS8604: Possible null reference argument for parameter 's' in 'void C.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void C.Goo(string s)").WithLocation(19, 13));
    }

    [Fact]
    public void ConstructorParameterWithoutNotNullIfNotNull_BothNotNull()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyList<T> : List<T>
            {
                public MyList(string? arg, string? other)
                {
                }
            }

            class C
            {
                static void Main()
                {
                    string? s = "";
                    string? other = "";
                    MyList<int> list = [with(s, other), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (19,13): warning CS8604: Possible null reference argument for parameter 's' in 'void C.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void C.Goo(string s)").WithLocation(19, 13));
    }

    #endregion


    [Fact]
    public void CollectionBuilderParameterWithNotNullIfNotNull_BothNull()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : IEnumerable<T>
            {
                private readonly List<T> _items;
                public MyCollection(string? value, string? other, ReadOnlySpan<T> items)
                {
                    _items = new();
                    _items.AddRange(items.ToArray());
                }
                public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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
                    string? s = null;
                    string? other = null;
                    MyCollection<int> c = [with(s, other), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        // When 'other' is null, NotNullIfNotNull doesn't guarantee 's' is not null
        CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (9,13): warning CS8604: Possible null reference argument for parameter 's' in 'void Program.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void Program.Goo(string s)").WithLocation(9, 13));
    }

    [Fact]
    public void CollectionBuilderParameterWithNotNullIfNotNull_OtherNotNull()
    {
        string sourceA = """
            #nullable enable
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : IEnumerable<T>
            {
                private readonly List<T> _items;
                public MyCollection(string? value, string? other, ReadOnlySpan<T> items)
                {
                    _items = new();
                    _items.AddRange(items.ToArray());
                }
                public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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
                    string? s = null;
                    string other = "";
                    MyCollection<int> c = [with(s, other), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (9,13): warning CS8604: Possible null reference argument for parameter 's' in 'void Program.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void Program.Goo(string s)").WithLocation(9, 13));
    }

    [Fact]
    public void CollectionBuilderParameterWithoutNotNullIfNotNull_OtherNotNull()
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
                public MyCollection(string? value, string? other, ReadOnlySpan<T> items)
                {
                    _items = new();
                    _items.AddRange(items.ToArray());
                }
                public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>(string? value, string? other, ReadOnlySpan<T> items)
                    => new(value, other, items);
            }
            """;
        string sourceB = """
            #nullable enable
            class Program
            {
                static void Main()
                {
                    string? s = null;
                    string other = "";
                    MyCollection<int> c = [with(s, other), 1, 2];
                    Goo(s);
                }

                static void Goo(string s) { }
            }
            """;

        // Without NotNullIfNotNull, even if 'other' is not null, 's' is still maybe null
        CompileAndVerify(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80,
            verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (9,13): warning CS8604: Possible null reference argument for parameter 's' in 'void Program.Goo(string s)'.
            //         Goo(s);
            Diagnostic(ErrorCode.WRN_NullReferenceArgument, "s").WithArguments("s", "void Program.Goo(string s)").WithLocation(9, 13));
    }
}
