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
public sealed class CollectionExpressionTests_WithElement_ArraysAndSpans : CSharpTestBase
{
    [Fact]
    public void WithElement_Array_Simple()
    {
        var source = """
            class C
            {
                void M()
                {
                    int[] array = [with(), 1, 2, 3];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (5,24): error CS9336: Collection arguments are not supported for type 'int[]'.
            //         int[] array = [with(), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("int[]").WithLocation(5, 24));
    }

    [Fact]
    public void WithElement_Array_WithArguments()
    {
        var source = """
            class C
            {
                void M()
                {
                    int[] array = [with(capacity: 10), 1, 2, 3];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (5,24): error CS9336: Collection arguments are not supported for type 'int[]'.
            //         int[] array = [with(capacity: 10), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("int[]").WithLocation(5, 24));
    }

    [Fact]
    public void WithElement_Array_Multidimensional()
    {
        var source = """
            class C
            {
                void M()
                {
                    int[,] array = [with()];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (5,24): error CS9174: Cannot initialize type 'int[*,*]' with a collection expression because the type is not constructible.
            //         int[,] array = [with()];
            Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[with()]").WithArguments("int[*,*]").WithLocation(5, 24));
    }

    [Fact]
    public void WithElement_Array_Jagged()
    {
        var source = """
            class C
            {
                void M()
                {
                    int[][] array = [with(), new int[] { 1, 2 }];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (5,26): error CS9336: Collection arguments are not supported for type 'int[][]'.
            //         int[][] array = [with(), new int[] { 1, 2 }];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("int[][]").WithLocation(5, 26));
    }

    [Fact]
    public void WithElement_Array_Empty()
    {
        var source = """
            class C
            {
                void M()
                {
                    int[] array = [with()];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (5,24): error CS9336: Collection arguments are not supported for type 'int[]'.
            //         int[] array = [with()];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("int[]").WithLocation(5, 24));
    }

    [Fact]
    public void WithElement_Array_WithNamedArguments()
    {
        var source = """
            class C
            {
                void M()
                {
                    string[] array = [with(capacity: 5, name: "test"), "hello", "world"];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (5,27): error CS9336: Collection arguments are not supported for type 'string[]'.
            //         string[] array = [with(capacity: 5, name: "test"), "hello", "world"];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("string[]").WithLocation(5, 27));
    }

    [Fact]
    public void WithElement_Span_Simple()
    {
        var source = """
            using System;
            
            class C
            {
                void M()
                {
                    Span<int> span = [with(), 1, 2, 3];
                }
            }
            """;

        CreateCompilation(source, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
            // (7,27): error CS9336: Collection arguments are not supported for type 'Span<int>'.
            //         Span<int> span = [with(), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("System.Span<int>").WithLocation(7, 27));
    }

    [Fact]
    public void WithElement_Span_WithArguments()
    {
        var source = """
            using System;
            
            class C
            {
                void M()
                {
                    Span<int> span = [with(capacity: 10), 1, 2, 3];
                }
            }
            """;

        CreateCompilation(source, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
            // (7,27): error CS9336: Collection arguments are not supported for type 'Span<int>'.
            //         Span<int> span = [with(capacity: 10), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("System.Span<int>").WithLocation(7, 27));
    }

    [Fact]
    public void WithElement_Span_Empty()
    {
        var source = """
            using System;
            
            class C
            {
                void M()
                {
                    Span<int> span = [with()];
                }
            }
            """;

        CreateCompilation(source, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
            // (7,27): error CS9336: Collection arguments are not supported for type 'Span<int>'.
            //         Span<int> span = [with()];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("System.Span<int>").WithLocation(7, 27));
    }

    [Fact]
    public void WithElement_Span_WithNamedArguments()
    {
        var source = """
            using System;
            
            class C
            {
                void M()
                {
                    Span<string> span = [with(size: 10), "hello", "world"];
                }
            }
            """;

        CreateCompilation(source, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
            // (7,30): error CS9336: Collection arguments are not supported for type 'Span<string>'.
            //         Span<string> span = [with(size: 10), "hello", "world"];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("System.Span<string>").WithLocation(7, 30));
    }

    [Fact]
    public void WithElement_ReadOnlySpan_Simple()
    {
        var source = """
            using System;
            
            class C
            {
                void M()
                {
                    ReadOnlySpan<int> span = [with(), 1, 2, 3];
                }
            }
            """;

        CreateCompilation(source, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
            // (7,35): error CS9336: Collection arguments are not supported for type 'ReadOnlySpan<int>'.
            //         ReadOnlySpan<int> span = [with(), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("System.ReadOnlySpan<int>").WithLocation(7, 35));
    }

    [Fact]
    public void WithElement_ReadOnlySpan_WithArguments()
    {
        var source = """
            using System;
            
            class C
            {
                void M()
                {
                    ReadOnlySpan<int> span = [with(capacity: 10), 1, 2, 3];
                }
            }
            """;

        CreateCompilation(source, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
            // (7,35): error CS9336: Collection arguments are not supported for type 'ReadOnlySpan<int>'.
            //         ReadOnlySpan<int> span = [with(capacity: 10), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("System.ReadOnlySpan<int>").WithLocation(7, 35));
    }

    [Fact]
    public void WithElement_ReadOnlySpan_Empty()
    {
        var source = """
            using System;
            
            class C
            {
                void M()
                {
                    ReadOnlySpan<int> span = [with()];
                }
            }
            """;

        CreateCompilation(source, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
            // (7,35): error CS9336: Collection arguments are not supported for type 'ReadOnlySpan<int>'.
            //         ReadOnlySpan<int> span = [with()];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("System.ReadOnlySpan<int>").WithLocation(7, 35));
    }

    [Fact]
    public void WithElement_ReadOnlySpan_WithNamedArguments()
    {
        var source = """
            using System;
            
            class C
            {
                void M()
                {
                    ReadOnlySpan<char> span = [with(length: 5), 'a', 'b', 'c'];
                }
            }
            """;

        CreateCompilation(source, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
            // (7,36): error CS9336: Collection arguments are not supported for type 'ReadOnlySpan<char>'.
            //         ReadOnlySpan<char> span = [with(length: 5), 'a', 'b', 'c'];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("System.ReadOnlySpan<char>").WithLocation(7, 36));
    }

    [Fact]
    public void WithElement_Array_NestedInGeneric()
    {
        var source = """
            using System.Collections.Generic;
            
            class C
            {
                void M()
                {
                    List<int[]> list = [[with(), 1, 2, 3]];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (7,30): error CS9336: Collection arguments are not supported for type 'int[]'.
            //         List<int[]> list = [[with(), 1, 2, 3]];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("int[]").WithLocation(7, 30));
    }

    [Fact]
    public void WithElement_Span_NestedInGeneric()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class C
            {
                void M()
                {
                    List<Span<int>> list = [[with(), 1, 2, 3]];
                }
            }
            """;

        CreateCompilation(source, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
            // (8,14): error CS9244: The type 'Span<int>' may not be a ref struct or a type parameter allowing ref structs in order to use it as parameter 'T' in the generic type or method 'List<T>'
            //         List<Span<int>> list = [[with(), 1, 2, 3]];
            Diagnostic(ErrorCode.ERR_NotRefStructConstraintNotSatisfied, "Span<int>").WithArguments("System.Collections.Generic.List<T>", "T", "System.Span<int>").WithLocation(8, 14),
            // (8,34): error CS9336: Collection arguments are not supported for type 'Span<int>'.
            //         List<Span<int>> list = [[with(), 1, 2, 3]];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("System.Span<int>").WithLocation(8, 34));
    }

    [Fact]
    public void WithElement_ReadOnlySpan_NestedInGeneric()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class C
            {
                void M()
                {
                    List<ReadOnlySpan<int>> list = [[with(), 1, 2, 3]];
                }
            }
            """;

        CreateCompilation(source, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
            // (8,14): error CS9244: The type 'ReadOnlySpan<int>' may not be a ref struct or a type parameter allowing ref structs in order to use it as parameter 'T' in the generic type or method 'List<T>'
            //         List<ReadOnlySpan<int>> list = [[with(), 1, 2, 3]];
            Diagnostic(ErrorCode.ERR_NotRefStructConstraintNotSatisfied, "ReadOnlySpan<int>").WithArguments("System.Collections.Generic.List<T>", "T", "System.ReadOnlySpan<int>").WithLocation(8, 14),
            // (8,42): error CS9336: Collection arguments are not supported for type 'ReadOnlySpan<int>'.
            //         List<ReadOnlySpan<int>> list = [[with(), 1, 2, 3]];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("System.ReadOnlySpan<int>").WithLocation(8, 42));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77827")]
    public void WithElement_RefLikeElementType1()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            class C
            {
                static void Main()
                {
                    IEnumerable<ReadOnlySpan<int>> list = [[1, 2, 3], [4, 5, 6]];

                    foreach (var span in list)
                    {
                        foreach (var item in span)
                        {
                            Console.Write(item + " ");
                        }
                    }
                }
            }
            """;

        CreateCompilation(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (8,47): error CS9404: Element type of an interface collection may not be a ref struct or a type parameter allowing ref structs
            //         IEnumerable<ReadOnlySpan<int>> list = [[1, 2, 3], [4, 5, 6]];
            Diagnostic(ErrorCode.ERR_CollectionRefLikeElementType, "[[1, 2, 3], [4, 5, 6]]").WithLocation(8, 47));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77827")]
    public void WithElement_RefLikeElementType2()
    {
        var source = """
            using System.Collections.Generic;
            
            class C
            {
                static void Goo<T>(T t1) where T : allows ref struct
                {
                    IEnumerable<T> list = [t1];
                }
            }
            """;

        CreateCompilation(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (7,31): error CS9404: Element type of an interface collection may not be a ref struct or a type parameter allowing ref structs
            //         IEnumerable<T> list = [t1];
            Diagnostic(ErrorCode.ERR_CollectionRefLikeElementType, "[t1]").WithLocation(7, 31));
    }

    [Fact]
    public void WithElement_Array_AsMethodParameter()
    {
        var source = """
            class C
            {
                void Method(int[] array) { }
                
                void M()
                {
                    Method([with(capacity: 10), 1, 2, 3]);
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (7,17): error CS9336: Collection arguments are not supported for type 'int[]'.
            //         Method([with(capacity: 10), 1, 2, 3]);
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("int[]").WithLocation(7, 17));
    }

    [Fact]
    public void WithElement_Span_AsMethodParameter()
    {
        var source = """
            using System;
            
            class C
            {
                void Method(Span<int> span) { }
                
                void M()
                {
                    Method([with(capacity: 10), 1, 2, 3]);
                }
            }
            """;

        CreateCompilation(source, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
            // (9,17): error CS9336: Collection arguments are not supported for type 'Span<int>'.
            //         Method([with(capacity: 10), 1, 2, 3]);
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("System.Span<int>").WithLocation(9, 17));
    }

    [Fact]
    public void WithElement_ReadOnlySpan_AsMethodParameter()
    {
        var source = """
            using System;
            
            class C
            {
                void Method(ReadOnlySpan<int> span) { }
                
                void M()
                {
                    Method([with(capacity: 10), 1, 2, 3]);
                }
            }
            """;

        CreateCompilation(source, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
            // (9,17): error CS9336: Collection arguments are not supported for type 'ReadOnlySpan<int>'.
            //         Method([with(capacity: 10), 1, 2, 3]);
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("System.ReadOnlySpan<int>").WithLocation(9, 17));
    }

    [Fact]
    public void WithElement_Array_AsReturnValue()
    {
        var source = """
            class C
            {
                int[] Method()
                {
                    return [with(), 1, 2, 3];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (5,17): error CS9336: Collection arguments are not supported for type 'int[]'.
            //         return [with(), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("int[]").WithLocation(5, 17));
    }

    [Fact]
    public void WithElement_Span_AsReturnValue()
    {
        var source = """
            using System;
            
            class C
            {
                Span<int> Method()
                {
                    return [with(), 1, 2, 3];
                }
            }
            """;

        CreateCompilation(source, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
            // (7,17): error CS9401: 'with(...)' elements are not supported for type 'Span<int>'
            //         return [with(), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("System.Span<int>").WithLocation(7, 17));
    }

    [Fact]
    public void WithElement_ReadOnlySpan_AsReturnValue()
    {
        var source = """
            using System;
            
            class C
            {
                ReadOnlySpan<int> Method()
                {
                    return [with(), 1, 2, 3];
                }
            }
            """;

        CreateCompilation(source, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
            // (7,17): error CS9336: Collection arguments are not supported for type 'ReadOnlySpan<int>'.
            //         return [with(), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("System.ReadOnlySpan<int>").WithLocation(7, 17));
    }

    [Fact]
    public void WithElement_Array_WithRefOutInParameters()
    {
        var source = """
            class C
            {
                void M()
                {
                    int x = 10;
                    int[] array = [with(ref x), 1, 2, 3];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (6,24): error CS9401: 'with(...)' elements are not supported for type 'int[]'
            //         int[] array = [with(ref x), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("int[]").WithLocation(6, 24));
    }

    [Fact]
    public void WithElement_Span_WithRefOutInParameters()
    {
        var source = """
            using System;
            
            class C
            {
                void M()
                {
                    int x = 10;
                    Span<int> span = [with(in x), 1, 2, 3];
                }
            }
            """;

        var compilation = CreateCompilation(source, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
            // (8,27): error CS9401: 'with(...)' elements are not supported for type 'Span<int>'
            //         Span<int> span = [with(in x), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("System.Span<int>").WithLocation(8, 27));
        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        compilation.VerifyOperationTree(root.DescendantNodes().OfType<CollectionExpressionSyntax>().Single(), """
            ICollectionExpressionOperation (3 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Span<System.Int32>, IsInvalid) (Syntax: '[with(in x), 1, 2, 3]')
              ConstructArguments(1):
                  ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
              Elements(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            """);
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/80518")]
    public void WithElement_ReadOnlySpan_WithOutVar()
    {
        var source = """
            using System;
            
            class C
            {
                void M()
                {
                    ReadOnlySpan<int> span = [with(out var x), x, x + 1];
                }
            }
            """;

        CreateCompilation(source, targetFramework: TargetFramework.Net80).VerifyDiagnostics();
    }

    [Fact]
    public void WithElement_Array_WithParamsArguments()
    {
        var source = """
            class C
            {
                void M()
                {
                    int[] array = [with(1, 2, 3, 4, 5), 6, 7, 8];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (5,24): error CS9336: Collection arguments are not supported for type 'int[]'.
            //         int[] array = [with(1, 2, 3, 4, 5), 6, 7, 8];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("int[]").WithLocation(5, 24));
    }

    [Fact]
    public void WithElement_Array_WithDynamicArguments()
    {
        var source = """
            class C
            {
                void M()
                {
                    dynamic d = 42;
                    int[] array = [with(d), 1, 2, 3];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (6,29): error CS9337: Collection arguments cannot be dynamic
            //         int[] array = [with(d), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, "d").WithLocation(6, 29));
    }

    [Fact]
    public void WithElement_Span_WithDynamicArguments()
    {
        var source = """
            using System;
            
            class C
            {
                void M()
                {
                    dynamic d = 42;
                    Span<int> span = [with(capacity: d), 1, 2, 3];
                }
            }
            """;

        CreateCompilation(source, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
            // (8,42): error CS9337: Collection arguments cannot be dynamic
            //         Span<int> span = [with(capacity: d), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, "d").WithLocation(8, 42));
    }

    [Fact]
    public void WithElement_Array_ImplicitlyTyped()
    {
        var source = """
            class C
            {
                void M()
                {
                    var array = [with(), 1, 2, 3] as int[];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (5,21): error CS9176: There is no target type for the collection expression.
            //         var array = [with(), 1, 2, 3] as int[];
            Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[with(), 1, 2, 3]").WithLocation(5, 21));
    }

    [Fact]
    public void WithElement_Span_ImplicitlyTyped()
    {
        var source = """
            using System;
            
            class C
            {
                void M()
                {
                    var span = [with(), 1, 2, 3] as Span<int>;
                }
            }
            """;

        CreateCompilation(source, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
            // (7,20): error CS9176: There is no target type for the collection expression.
            //         var span = [with(), 1, 2, 3] as Span<int>;
            Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[with(), 1, 2, 3]").WithLocation(7, 20));
    }

    [Fact]
    public void WithElement_ReadOnlySpan_ImplicitlyTyped()
    {
        var source = """
            using System;
            
            class C
            {
                void M()
                {
                    var span = [with(), 1, 2, 3] as ReadOnlySpan<int>;
                }
            }
            """;

        CreateCompilation(source, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
            // (7,20): error CS9176: There is no target type for the collection expression.
            //         var span = [with(), 1, 2, 3] as ReadOnlySpan<int>;
            Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[with(), 1, 2, 3]").WithLocation(7, 20));
    }

    [Fact]
    public void WithElement_Array_WithGenericType()
    {
        var source = """
            class C<T>
            {
                void M()
                {
                    T[] array = [with()];
                }
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (5,22): error CS9336: Collection arguments are not supported for type 'T[]'.
            //         T[] array = [with()];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("T[]").WithLocation(5, 22));
    }

    [Fact]
    public void WithElement_Span_WithGenericType()
    {
        var source = """
            using System;
            
            class C<T>
            {
                void M()
                {
                    Span<T> span = [with()];
                }
            }
            """;

        CreateCompilation(source, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
            // (7,25): error CS9336: Collection arguments are not supported for type 'Span<T>'.
            //         Span<T> span = [with()];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("System.Span<T>").WithLocation(7, 25));
    }

    [Fact]
    public void WithElement_ReadOnlySpan_WithGenericType()
    {
        var source = """
            using System;
            
            class C<T>
            {
                void M()
                {
                    ReadOnlySpan<T> span = [with()];
                }
            }
            """;

        CreateCompilation(source, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
            // (7,33): error CS9336: Collection arguments are not supported for type 'ReadOnlySpan<T>'.
            //         ReadOnlySpan<T> span = [with()];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("System.ReadOnlySpan<T>").WithLocation(7, 33));
    }

    [Theory]
    [InlineData("string[]")]
    [InlineData("System.ReadOnlySpan<string>")]
    public void WithElement_GetSymbolInfo(string type)
    {
        var source = $$"""
            class C
            {
                static void Main()
                {
                    string s = "";
                    {{type}} list = [with(), s];
                }
            }
            """;

        var compilation = CreateCompilation(source, targetFramework: TargetFramework.Net100).VerifyDiagnostics(
            // (6,45): error CS9401: 'with(...)' elements are not supported for type 'ReadOnlySpan<string>'
            //         System.ReadOnlySpan<string> list = [with(), s];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments(type));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        var withElement = tree.GetRoot().DescendantNodes().OfType<WithElementSyntax>().Single();

        var symbol = semanticModel.GetSymbolInfo(withElement);
        Assert.Null(symbol.Symbol);
        Assert.Empty(symbol.CandidateSymbols);
    }
}
