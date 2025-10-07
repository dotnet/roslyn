// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

<<<<<<< HEAD
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
=======
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
>>>>>>> upstream/features/collection-expression-arguments
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

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

    [Fact]
<<<<<<< HEAD
    public void WithElement_ReadOnlySpan_NestedInGeneric2()
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

        // PROTOTYPE: This should be blocked.  We generate code that fails to load.
        // https://github.com/dotnet/roslyn/issues/77827
        CompileAndVerify(source, targetFramework: TargetFramework.Net90, verify: Verification.Fails).VerifyIL("C.Main", """
            {
              // Code size      131 (0x83)
              .maxstack  4
              .locals init (System.Collections.Generic.IEnumerator<System.ReadOnlySpan<int>> V_0,
                            System.ReadOnlySpan<int> V_1,
                            int V_2,
                            int V_3) //item
              IL_0000:  ldc.i4.2
              IL_0001:  newarr     "System.ReadOnlySpan<int>"
              IL_0006:  dup
              IL_0007:  ldc.i4.0
              IL_0008:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12_Align=4 <PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D4"
              IL_000d:  call       "System.ReadOnlySpan<int> System.Runtime.CompilerServices.RuntimeHelpers.CreateSpan<int>(System.RuntimeFieldHandle)"
              IL_0012:  stelem     "System.ReadOnlySpan<int>"
              IL_0017:  dup
              IL_0018:  ldc.i4.1
              IL_0019:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12_Align=4 <PrivateImplementationDetails>.8CA6EE1043DEFCFD05AA29DEE581CBC519E783E414A687D7C26AC6070D3F6DEE4"
              IL_001e:  call       "System.ReadOnlySpan<int> System.Runtime.CompilerServices.RuntimeHelpers.CreateSpan<int>(System.RuntimeFieldHandle)"
              IL_0023:  stelem     "System.ReadOnlySpan<int>"
              IL_0028:  newobj     "<>z__ReadOnlyArray<System.ReadOnlySpan<int>>..ctor(System.ReadOnlySpan<int>[])"
              IL_002d:  callvirt   "System.Collections.Generic.IEnumerator<System.ReadOnlySpan<int>> System.Collections.Generic.IEnumerable<System.ReadOnlySpan<int>>.GetEnumerator()"
              IL_0032:  stloc.0
              .try
              {
                IL_0033:  br.s       IL_006e
                IL_0035:  ldloc.0
                IL_0036:  callvirt   "System.ReadOnlySpan<int> System.Collections.Generic.IEnumerator<System.ReadOnlySpan<int>>.Current.get"
                IL_003b:  stloc.1
                IL_003c:  ldc.i4.0
                IL_003d:  stloc.2
                IL_003e:  br.s       IL_0064
                IL_0040:  ldloca.s   V_1
                IL_0042:  ldloc.2
                IL_0043:  call       "ref readonly int System.ReadOnlySpan<int>.this[int].get"
                IL_0048:  ldind.i4
                IL_0049:  stloc.3
                IL_004a:  ldloca.s   V_3
                IL_004c:  call       "string int.ToString()"
                IL_0051:  ldstr      " "
                IL_0056:  call       "string string.Concat(string, string)"
                IL_005b:  call       "void System.Console.Write(string)"
                IL_0060:  ldloc.2
                IL_0061:  ldc.i4.1
                IL_0062:  add
                IL_0063:  stloc.2
                IL_0064:  ldloc.2
                IL_0065:  ldloca.s   V_1
                IL_0067:  call       "int System.ReadOnlySpan<int>.Length.get"
                IL_006c:  blt.s      IL_0040
                IL_006e:  ldloc.0
                IL_006f:  callvirt   "bool System.Collections.IEnumerator.MoveNext()"
                IL_0074:  brtrue.s   IL_0035
                IL_0076:  leave.s    IL_0082
              }
              finally
              {
                IL_0078:  ldloc.0
                IL_0079:  brfalse.s  IL_0081
                IL_007b:  ldloc.0
                IL_007c:  callvirt   "void System.IDisposable.Dispose()"
                IL_0081:  endfinally
              }
              IL_0082:  ret
            }
            """);
    }

    [Fact]
=======
>>>>>>> upstream/features/collection-expression-arguments
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
            // (7,16): error CS9203: A collection expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the current scope.
            //         return [with(), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_CollectionExpressionEscape, "[with(), 1, 2, 3]").WithArguments("System.Span<int>").WithLocation(7, 16),
            // (7,17): error CS9336: Collection arguments are not supported for type 'Span<int>'.
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
            // (5,13): warning CS0219: The variable 'x' is assigned but its value is never used
            //         int x = 10;
            Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(5, 13),
            // (6,24): error CS9336: Collection arguments are not supported for type 'int[]'.
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

        CreateCompilation(source, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
            // (7,13): warning CS0219: The variable 'x' is assigned but its value is never used
            //         int x = 10;
            Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(7, 13),
            // (8,27): error CS9336: Collection arguments are not supported for type 'Span<int>'.
            //         Span<int> span = [with(in x), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("System.Span<int>").WithLocation(8, 27));
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
}
