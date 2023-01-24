// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class CollectionLiteralTests : CSharpTestBase
    {
        [Theory]
        [InlineData(LanguageVersion.CSharp11)]
        [InlineData(LanguageVersion.Preview)]
        public void LanguageVersionDiagnostics(LanguageVersion languageVersion)
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        List<object> l;
                        l = [];
                        l = [1, 2, 3];
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion == LanguageVersion.CSharp11)
            {
                comp.VerifyEmitDiagnostics(
                    // (7,13): error CS8652: The feature 'collection literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         l = [];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "[").WithArguments("collection literals").WithLocation(7, 13),
                    // (8,13): error CS8652: The feature 'collection literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         l = [1, 2, 3];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "[").WithArguments("collection literals").WithLocation(8, 13));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Fact]
        public void Array()
        {
            string source = """
                using System;
                class Program
                {
                    static void Main()
                    {
                        int[] a = Create();
                        Console.WriteLine((a.Length, a[0], a[1]));
                    }
                    static int[] Create() => [2, 3];
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "(2, 2, 3)");
            verifier.VerifyIL("Program.Create", """
                {
                  // Code size       90 (0x5a)
                  .maxstack  2
                  .locals init (System.Collections.Generic.List<object> V_0,
                                System.Collections.Generic.List<int>.Enumerator V_1,
                                int V_2)
                  IL_0000:  newobj     "System.Collections.Generic.List<object>..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldloc.0
                  IL_0007:  ldc.i4.1
                  IL_0008:  box        "int"
                  IL_000d:  callvirt   "void System.Collections.Generic.List<object>.Add(object)"
                  IL_0012:  call       "System.Collections.Generic.List<int> Program.Create2()"
                  IL_0017:  callvirt   "System.Collections.Generic.List<int>.Enumerator System.Collections.Generic.List<int>.GetEnumerator()"
                  IL_001c:  stloc.1
                  .try
                  {
                    IL_001d:  br.s       IL_0033
                    IL_001f:  ldloca.s   V_1
                    IL_0021:  call       "int System.Collections.Generic.List<int>.Enumerator.Current.get"
                    IL_0026:  stloc.2
                    IL_0027:  ldloc.0
                    IL_0028:  ldloc.2
                    IL_0029:  box        "int"
                    IL_002e:  callvirt   "void System.Collections.Generic.List<object>.Add(object)"
                    IL_0033:  ldloca.s   V_1
                    IL_0035:  call       "bool System.Collections.Generic.List<int>.Enumerator.MoveNext()"
                    IL_003a:  brtrue.s   IL_001f
                    IL_003c:  leave.s    IL_004c
                  }
                  finally
                  {
                    IL_003e:  ldloca.s   V_1
                    IL_0040:  constrained. "System.Collections.Generic.List<int>.Enumerator"
                    IL_0046:  callvirt   "void System.IDisposable.Dispose()"
                    IL_004b:  endfinally
                  }
                  IL_004c:  ldloc.0
                  IL_004d:  ldc.i4.2
                  IL_004e:  box        "int"
                  IL_0053:  callvirt   "void System.Collections.Generic.List<object>.Add(object)"
                  IL_0058:  ldloc.0
                  IL_0059:  ret
                }
                """);
        }

        // PROTOTYPE: Test with type that implements IEnumerable, not IEnumerable<T>.
        // PROTOTYPE: Test with different collection types: class, struct, array, string, etc.
        // PROTOTYPE: Test with types that are not constructible: non-collection type, static type, interface, abstract type, type parameter, etc.
        // PROTOTYPE: Test with type parameter T where T : new(), IEnumerable<U>, and with struct, class, or neither constraint.
        // PROTOTYPE: Test with explicit cast rather than target type, with collection type, or base type, etc.
        // PROTOTYPE: Test with Nullable<T> where T is a collection type. See also LocalRewriter.VisitConversion() which has special handling for ConversionKind.ObjectCreation with Nullable<T>.
        // PROTOTYPE: Test with missing constructor; inaccessible constructor; implicit (default) constructor; constructor with unexpected parameters; constructor with optional parameters; constructor with params parameter.
        [Fact]
        public void CollectionInitializerType_Empty()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        List<long?> c = Create();
                        Console.WriteLine(c.Count);
                    }
                    static List<long?> Create() => [];
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "0");
            verifier.VerifyIL("Program.Create", """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  newobj     "System.Collections.Generic.List<long?>..ctor()"
                  IL_0005:  ret
                }
                """);
        }

        [Fact]
        public void CollectionInitializerType_WithElements()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        List<long?> c = Create();
                        Console.WriteLine((c.Count, c[0], c[1] is null));
                    }
                    static List<long?> Create() => [1, null];
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "(2, 1, True)");
            verifier.VerifyIL("Program.Create", """
                {
                  // Code size       34 (0x22)
                  .maxstack  3
                  .locals init (long? V_0)
                  IL_0000:  newobj     "System.Collections.Generic.List<long?>..ctor()"
                  IL_0005:  dup
                  IL_0006:  ldc.i4.1
                  IL_0007:  conv.i8
                  IL_0008:  newobj     "long?..ctor(long)"
                  IL_000d:  callvirt   "void System.Collections.Generic.List<long?>.Add(long?)"
                  IL_0012:  dup
                  IL_0013:  ldloca.s   V_0
                  IL_0015:  initobj    "long?"
                  IL_001b:  ldloc.0
                  IL_001c:  callvirt   "void System.Collections.Generic.List<long?>.Add(long?)"
                  IL_0021:  ret
                }
                """);
        }

        [Fact]
        public void DictionaryElements_01()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Dictionary<object, object> d = Create();
                        Console.WriteLine((d.Count, d[1]));
                    }
                    static Dictionary<object, object> Create() => [1:2];
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "(1, 2)");
            verifier.VerifyIL("Program.Create", """
                {
                  // Code size       24 (0x18)
                  .maxstack  4
                  IL_0000:  newobj     "System.Collections.Generic.Dictionary<object, object>..ctor()"
                  IL_0005:  dup
                  IL_0006:  ldc.i4.1
                  IL_0007:  box        "int"
                  IL_000c:  ldc.i4.2
                  IL_000d:  box        "int"
                  IL_0012:  callvirt   "void System.Collections.Generic.Dictionary<object, object>.this[object].set"
                  IL_0017:  ret
                }
                """);
        }

        [Fact]
        public void DictionaryElements_02()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static Dictionary<string, int> Create(object k, object v) => [k:v];
                }
                """;
            var comp = CreateCompilation(source);
            // PROTOTYPE: Conversion errors should highlight k or v not k:v.
            comp.VerifyEmitDiagnostics(
                // (4,67): error CS0266: Cannot implicitly convert type 'object' to 'string'. An explicit conversion exists (are you missing a cast?)
                //     static Dictionary<string, int> Create(object k, object v) => [k:v];
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "k:v").WithArguments("object", "string").WithLocation(4, 67),
                // (4,67): error CS0266: Cannot implicitly convert type 'object' to 'int'. An explicit conversion exists (are you missing a cast?)
                //     static Dictionary<string, int> Create(object k, object v) => [k:v];
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "k:v").WithArguments("object", "int").WithLocation(4, 67));
        }

        [Fact]
        public void DictionaryElements_03()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Dictionary<int, object> d = Create();
                        Console.WriteLine((d.Count, d[1], d[2]));
                    }
                    static Dictionary<int, object> Create() => [1:1, 2:null, new KeyValuePair<int, object>(2, "3"), 1:4];
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "(2, 4, 3)");
            verifier.VerifyIL("Program.Create", """
                {
                  // Code size       84 (0x54)
                  .maxstack  5
                  .locals init (System.Collections.Generic.KeyValuePair<int, object> V_0)
                  IL_0000:  newobj     "System.Collections.Generic.Dictionary<int, object>..ctor()"
                  IL_0005:  dup
                  IL_0006:  ldc.i4.1
                  IL_0007:  ldc.i4.1
                  IL_0008:  box        "int"
                  IL_000d:  callvirt   "void System.Collections.Generic.Dictionary<int, object>.this[int].set"
                  IL_0012:  dup
                  IL_0013:  ldc.i4.2
                  IL_0014:  ldnull
                  IL_0015:  callvirt   "void System.Collections.Generic.Dictionary<int, object>.this[int].set"
                  IL_001a:  dup
                  IL_001b:  ldc.i4.2
                  IL_001c:  ldstr      "3"
                  IL_0021:  newobj     "System.Collections.Generic.KeyValuePair<int, object>..ctor(int, object)"
                  IL_0026:  stloc.0
                  IL_0027:  ldloca.s   V_0
                  IL_0029:  call       "int System.Collections.Generic.KeyValuePair<int, object>.Key.get"
                  IL_002e:  ldc.i4.2
                  IL_002f:  ldstr      "3"
                  IL_0034:  newobj     "System.Collections.Generic.KeyValuePair<int, object>..ctor(int, object)"
                  IL_0039:  stloc.0
                  IL_003a:  ldloca.s   V_0
                  IL_003c:  call       "object System.Collections.Generic.KeyValuePair<int, object>.Value.get"
                  IL_0041:  callvirt   "void System.Collections.Generic.Dictionary<int, object>.this[int].set"
                  IL_0046:  dup
                  IL_0047:  ldc.i4.1
                  IL_0048:  ldc.i4.4
                  IL_0049:  box        "int"
                  IL_004e:  callvirt   "void System.Collections.Generic.Dictionary<int, object>.this[int].set"
                  IL_0053:  ret
                }
                """);
        }

        // PROTOTYPE: Test type with TValue this[TKey] indexer but no Add(KeyValuePair<TKey, TValue>) method, and without a definition for KeyValuePair<TKey, TValue>. It should compile without errors.
        // PROTOTYPE: Test type with TValue this[TKey] indexer and Add(KeyValuePair<TKey, TValue>) method, and without a definition for KeyValuePair<TKey, TValue>. It should bind to Add() but report a use-site error.
        // PROTOTYPE: Test type with Add(KeyValuePair<TKey, TValue>) method with or without corresponding TValue this[TKey] indexer. Test adding k:v and e.
        // PROTOTYPE: Test type with neither Add(KeyValuePair<TKey, TValue>) or TValue this[TKey] indexer. Test adding k:v and e.
        // PROTOTYPE: Test type with multiple Add(KeyValuePair<TKey, TValue>) overloads, with different generic type arguments.
        // PROTOTYPE: Test type with multiple TValue this[TKey] indexer overloads, with different generic type arguments.
        // PROTOTYPE: Test all the many variations when the collection target type is dynamic - see BindCollectionInitializerElementAddMethod.
        // PROTOTYPE: Test with 'in' and 'ref' key parameter.
        // PROTOTYPE: Test with readonly indexer.
        // PROTOTYPE: Test with ref returning indexer.
        // PROTOTYPE: Test collection literal construction in expression trees.
        // PROTOTYPE: Test nullable analysis for k:v, e, and ..e.
        // PROTOTYPE: Test ref safety analysis for k:v, e, and ..e.
        // PROTOTYPE: Test side effects from each expression in e, k:v, ..e are executed exactly once.

        // PROTOTYPE: Determine natural type from mix of expression, dictionary, and spread elements.

        [Fact]
        public void Spread_List()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        List<object> c = Create1();
                        Console.WriteLine((c.Count, c[0], c[1], c[2], c[3]));
                    }
                    static List<object> Create1() => [1, ..Create2(), 2];
                    static List<int> Create2() => [3, 4];
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "(4, 1, 3, 4, 2)");
            verifier.VerifyIL("Program.Create1", """
                {
                  // Code size       90 (0x5a)
                  .maxstack  2
                  .locals init (System.Collections.Generic.List<object> V_0,
                                System.Collections.Generic.List<int>.Enumerator V_1,
                                int V_2)
                  IL_0000:  newobj     "System.Collections.Generic.List<object>..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldloc.0
                  IL_0007:  ldc.i4.1
                  IL_0008:  box        "int"
                  IL_000d:  callvirt   "void System.Collections.Generic.List<object>.Add(object)"
                  IL_0012:  call       "System.Collections.Generic.List<int> Program.Create2()"
                  IL_0017:  callvirt   "System.Collections.Generic.List<int>.Enumerator System.Collections.Generic.List<int>.GetEnumerator()"
                  IL_001c:  stloc.1
                  .try
                  {
                    IL_001d:  br.s       IL_0033
                    IL_001f:  ldloca.s   V_1
                    IL_0021:  call       "int System.Collections.Generic.List<int>.Enumerator.Current.get"
                    IL_0026:  stloc.2
                    IL_0027:  ldloc.0
                    IL_0028:  ldloc.2
                    IL_0029:  box        "int"
                    IL_002e:  callvirt   "void System.Collections.Generic.List<object>.Add(object)"
                    IL_0033:  ldloca.s   V_1
                    IL_0035:  call       "bool System.Collections.Generic.List<int>.Enumerator.MoveNext()"
                    IL_003a:  brtrue.s   IL_001f
                    IL_003c:  leave.s    IL_004c
                  }
                  finally
                  {
                    IL_003e:  ldloca.s   V_1
                    IL_0040:  constrained. "System.Collections.Generic.List<int>.Enumerator"
                    IL_0046:  callvirt   "void System.IDisposable.Dispose()"
                    IL_004b:  endfinally
                  }
                  IL_004c:  ldloc.0
                  IL_004d:  ldc.i4.2
                  IL_004e:  box        "int"
                  IL_0053:  callvirt   "void System.Collections.Generic.List<object>.Add(object)"
                  IL_0058:  ldloc.0
                  IL_0059:  ret
                }
                """);
        }

        [Fact]
        public void Spread_Dictionary()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Dictionary<int, object> d = Create();
                        Console.WriteLine((c.Count, c[1], c[2], c[3], c[4]));
                    }
                    static Dictionary<int, object> Create1() => [1:1, ..Create2(), 2:null];
                    static Dictionary<int, object> Create2() => [3:3, 4:4];
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "(4, 1, , 3, 4)");
            verifier.VerifyIL("Program.Create1", """
                {
                  // Code size       90 (0x5a)
                  .maxstack  2
                  .locals init (System.Collections.Generic.List<object> V_0,
                                System.Collections.Generic.List<int>.Enumerator V_1,
                                int V_2)
                  IL_0000:  newobj     "System.Collections.Generic.List<object>..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldloc.0
                  IL_0007:  ldc.i4.1
                  IL_0008:  box        "int"
                  IL_000d:  callvirt   "void System.Collections.Generic.List<object>.Add(object)"
                  IL_0012:  call       "System.Collections.Generic.List<int> Program.Create2()"
                  IL_0017:  callvirt   "System.Collections.Generic.List<int>.Enumerator System.Collections.Generic.List<int>.GetEnumerator()"
                  IL_001c:  stloc.1
                  .try
                  {
                    IL_001d:  br.s       IL_0033
                    IL_001f:  ldloca.s   V_1
                    IL_0021:  call       "int System.Collections.Generic.List<int>.Enumerator.Current.get"
                    IL_0026:  stloc.2
                    IL_0027:  ldloc.0
                    IL_0028:  ldloc.2
                    IL_0029:  box        "int"
                    IL_002e:  callvirt   "void System.Collections.Generic.List<object>.Add(object)"
                    IL_0033:  ldloca.s   V_1
                    IL_0035:  call       "bool System.Collections.Generic.List<int>.Enumerator.MoveNext()"
                    IL_003a:  brtrue.s   IL_001f
                    IL_003c:  leave.s    IL_004c
                  }
                  finally
                  {
                    IL_003e:  ldloca.s   V_1
                    IL_0040:  constrained. "System.Collections.Generic.List<int>.Enumerator"
                    IL_0046:  callvirt   "void System.IDisposable.Dispose()"
                    IL_004b:  endfinally
                  }
                  IL_004c:  ldloc.0
                  IL_004d:  ldc.i4.2
                  IL_004e:  box        "int"
                  IL_0053:  callvirt   "void System.Collections.Generic.List<object>.Add(object)"
                  IL_0058:  ldloc.0
                  IL_0059:  ret
                }
                """);
        }

        [ConditionalFact(typeof(ClrOnly))]
        public void Spread_Dynamic()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        List<object> c = Create1();
                        Console.WriteLine((c.Count, c[0], c[1], c[2], c[3]));
                    }
                    static List<object> Create1() => [1, ..Create2(), 2];
                    static dynamic Create2() => new[] { 3, 4 };
                }
                """;
            var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net70, expectedOutput: "(4, 1, 3, 4, 2)");
            verifier.VerifyIL("Program.Create1", """
                {
                  // Code size      149 (0x95)
                  .maxstack  3
                  .locals init (System.Collections.Generic.List<object> V_0,
                                System.Collections.IEnumerator V_1,
                                object V_2,
                                System.IDisposable V_3)
                  IL_0000:  newobj     "System.Collections.Generic.List<object>..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldloc.0
                  IL_0007:  ldc.i4.1
                  IL_0008:  box        "int"
                  IL_000d:  callvirt   "void System.Collections.Generic.List<object>.Add(object)"
                  IL_0012:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Collections.IEnumerable>> Program.<>o__1.<>p__0"
                  IL_0017:  brtrue.s   IL_003d
                  IL_0019:  ldc.i4.0
                  IL_001a:  ldtoken    "System.Collections.IEnumerable"
                  IL_001f:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
                  IL_0024:  ldtoken    "Program"
                  IL_0029:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
                  IL_002e:  call       "System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)"
                  IL_0033:  call       "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Collections.IEnumerable>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Collections.IEnumerable>>.Create(System.Runtime.CompilerServices.CallSiteBinder)"
                  IL_0038:  stsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Collections.IEnumerable>> Program.<>o__1.<>p__0"
                  IL_003d:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Collections.IEnumerable>> Program.<>o__1.<>p__0"
                  IL_0042:  ldfld      "System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Collections.IEnumerable> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Collections.IEnumerable>>.Target"
                  IL_0047:  ldsfld     "System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Collections.IEnumerable>> Program.<>o__1.<>p__0"
                  IL_004c:  call       "dynamic Program.Create2()"
                  IL_0051:  callvirt   "System.Collections.IEnumerable System.Func<System.Runtime.CompilerServices.CallSite, dynamic, System.Collections.IEnumerable>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)"
                  IL_0056:  callvirt   "System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()"
                  IL_005b:  stloc.1
                  .try
                  {
                    IL_005c:  br.s       IL_006c
                    IL_005e:  ldloc.1
                    IL_005f:  callvirt   "object System.Collections.IEnumerator.Current.get"
                    IL_0064:  stloc.2
                    IL_0065:  ldloc.0
                    IL_0066:  ldloc.2
                    IL_0067:  callvirt   "void System.Collections.Generic.List<object>.Add(object)"
                    IL_006c:  ldloc.1
                    IL_006d:  callvirt   "bool System.Collections.IEnumerator.MoveNext()"
                    IL_0072:  brtrue.s   IL_005e
                    IL_0074:  leave.s    IL_0087
                  }
                  finally
                  {
                    IL_0076:  ldloc.1
                    IL_0077:  isinst     "System.IDisposable"
                    IL_007c:  stloc.3
                    IL_007d:  ldloc.3
                    IL_007e:  brfalse.s  IL_0086
                    IL_0080:  ldloc.3
                    IL_0081:  callvirt   "void System.IDisposable.Dispose()"
                    IL_0086:  endfinally
                  }
                  IL_0087:  ldloc.0
                  IL_0088:  ldc.i4.2
                  IL_0089:  box        "int"
                  IL_008e:  callvirt   "void System.Collections.Generic.List<object>.Add(object)"
                  IL_0093:  ldloc.0
                  IL_0094:  ret
                }
                """);
        }

        // PROTOTYPE: Test when Create2() returns IEnumerable<int> instead.
        [Fact]
        public void Spread_IntoArray()
        {
            string source = """
                using System;
                class Program
                {
                    static void Main()
                    {
                        object[] a = Create1();
                        Console.WriteLine((a.Length, a[0], a[1], a[2], a[3]));
                    }
                    static object[] Create1() => [1, ..Create2(), 2];
                    static int[] Create2() => [3, 4];
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "(4, 1, 3, 4, 2)");
            verifier.VerifyIL("Program.Create1", """
                {
                  // Code size       90 (0x5a)
                  .maxstack  2
                  .locals init (System.Collections.Generic.List<object> V_0,
                                System.Collections.Generic.List<int>.Enumerator V_1,
                                int V_2)
                  IL_0000:  newobj     "System.Collections.Generic.List<object>..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldloc.0
                  IL_0007:  ldc.i4.1
                  IL_0008:  box        "int"
                  IL_000d:  callvirt   "void System.Collections.Generic.List<object>.Add(object)"
                  IL_0012:  call       "System.Collections.Generic.List<int> Program.Create2()"
                  IL_0017:  callvirt   "System.Collections.Generic.List<int>.Enumerator System.Collections.Generic.List<int>.GetEnumerator()"
                  IL_001c:  stloc.1
                  .try
                  {
                    IL_001d:  br.s       IL_0033
                    IL_001f:  ldloca.s   V_1
                    IL_0021:  call       "int System.Collections.Generic.List<int>.Enumerator.Current.get"
                    IL_0026:  stloc.2
                    IL_0027:  ldloc.0
                    IL_0028:  ldloc.2
                    IL_0029:  box        "int"
                    IL_002e:  callvirt   "void System.Collections.Generic.List<object>.Add(object)"
                    IL_0033:  ldloca.s   V_1
                    IL_0035:  call       "bool System.Collections.Generic.List<int>.Enumerator.MoveNext()"
                    IL_003a:  brtrue.s   IL_001f
                    IL_003c:  leave.s    IL_004c
                  }
                  finally
                  {
                    IL_003e:  ldloca.s   V_1
                    IL_0040:  constrained. "System.Collections.Generic.List<int>.Enumerator"
                    IL_0046:  callvirt   "void System.IDisposable.Dispose()"
                    IL_004b:  endfinally
                  }
                  IL_004c:  ldloc.0
                  IL_004d:  ldc.i4.2
                  IL_004e:  box        "int"
                  IL_0053:  callvirt   "void System.Collections.Generic.List<object>.Add(object)"
                  IL_0058:  ldloc.0
                  IL_0059:  ret
                }
                """);
        }

        // PROTOTYPE: Test spread elements that implement IEnumerable or IEnumerable pattern but not IEnumerable<T>.
        // PROTOTYPE: Test spread elements that are dynamic type - test with non-dictionary and dictionary targets.
        // PROTOTYPE: Review ForEachLoopBinder.BindForEachPartsWorker() and add appropriate tests for the various scenarios
        // including error reporting, conversions, pattern-based, extensions, Dispose(), etc.
        // PROTOTYPE: Test collection types that have special handlingn in LocalRewriter.VisitForEachStatement: single- and multi-dimension arrays, string, Span<T>, ReadOnlySpan<T>.

        [Fact]
        public void NotConstructibleType_Empty()
        {
            string source = """
                C c = [];
                class C { }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (1,7): error CS9105: Cannot initialize type 'C' with a collection literal because the type is not constructible.
                // C c = [];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[]").WithArguments("C").WithLocation(1, 7));
        }

        [Fact]
        public void CollectionInitializerType_Struct()
        {
            string source = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                struct S1<T> : IEnumerable<T>
                {
                    public void Add(T t) { }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                struct S2<T> : IEnumerable<T>
                {
                    public S2() { }
                    public void Add(T t) { }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                class Program
                {
                    static void M1()
                    {
                        S1<int> s;
                        s = [];
                        s = [1, 2];
                    }
                    static void M2()
                    {
                        S2<int> s;
                        s = [];
                        s = [1, 2];
                    }
                }
                """;
            var verifier = CompileAndVerify(source);
            verifier.VerifyIL("Program.M1", """
                {
                  // Code size       25 (0x19)
                  .maxstack  2
                  .locals init (S1<int> V_0)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "S1<int>"
                  IL_0008:  ldloca.s   V_0
                  IL_000a:  ldc.i4.1
                  IL_000b:  call       "void S1<int>.Add(int)"
                  IL_0010:  ldloca.s   V_0
                  IL_0012:  ldc.i4.2
                  IL_0013:  call       "void S1<int>.Add(int)"
                  IL_0018:  ret
                }
                """);
            verifier.VerifyIL("Program.M2", """
                {
                  // Code size       30 (0x1e)
                  .maxstack  2
                  .locals init (S2<int> V_0)
                  IL_0000:  newobj     "S2<int>..ctor()"
                  IL_0005:  pop
                  IL_0006:  ldloca.s   V_0
                  IL_0008:  call       "S2<int>..ctor()"
                  IL_000d:  ldloca.s   V_0
                  IL_000f:  ldc.i4.1
                  IL_0010:  call       "void S2<int>.Add(int)"
                  IL_0015:  ldloca.s   V_0
                  IL_0017:  ldc.i4.2
                  IL_0018:  call       "void S2<int>.Add(int)"
                  IL_001d:  ret
                }
                """);
        }

        [ConditionalFact(typeof(ClrOnly))]
        public void ImmutableArray_Empty()
        {
            string source = """
                using System;
                using System.Collections.Immutable;
                class Program
                {
                    static void Main()
                    {
                        ImmutableArray<long?> c = Create();
                        Console.WriteLine((c.Length, c.IsEmpty));
                    }
                    static ImmutableArray<long?> Create() => [];
                }
                """;
            var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net70, expectedOutput: "(0, True)");
            verifier.VerifyIL("Program.Create", """
                {
                    ...
                }
                """);
        }

        [ConditionalFact(typeof(ClrOnly))]
        public void ImmutableArray_WithElements()
        {
            string source = """
                using System;
                using System.Collections.Immutable;
                class Program
                {
                    static void Main()
                    {
                        ImmutableArray<long?> c = Create();
                        Console.WriteLine((c.Length, c[0], c[1] is null));
                    }
                    static ImmutableArray<long?> Create() => [1, null];
                }
                """;
            var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net70, expectedOutput: "(2, 1, True)");
            verifier.VerifyIL("Program.Create", """
                {
                    ...
                }
                """);
        }
    }
}
