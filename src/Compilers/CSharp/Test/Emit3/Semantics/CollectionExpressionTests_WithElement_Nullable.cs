// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

[CompilerTrait(CompilerFeature.CollectionExpressions)]
public sealed class CollectionExpressionTests_WithElement_Nullable : CSharpTestBase
{
    private static string? IncludeExpectedOutput(string expectedOutput) => ExecutionConditionUtil.IsMonoOrCoreClr ? expectedOutput : null;

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

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("1")).VerifyDiagnostics();
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
                    Goo([with(null), ""]);
                }

                static void Goo<T>(MyList<T> list)
                {
                    Console.WriteLine(list.Count);
                }
            }
            """;

        CompileAndVerify(source, expectedOutput: IncludeExpectedOutput("1")).VerifyDiagnostics();
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
            verify: Verification.Fails).VerifyDiagnostics(
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
            verify: Verification.Fails).VerifyDiagnostics(
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
            verify: Verification.Fails).VerifyDiagnostics();
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
            verify: Verification.Fails).VerifyDiagnostics(
            // (7,17): warning CS0219: The variable 's' is assigned but its value is never used
            //         string? s = null;
            Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s").WithArguments("s").WithLocation(7, 17));
    }
}
