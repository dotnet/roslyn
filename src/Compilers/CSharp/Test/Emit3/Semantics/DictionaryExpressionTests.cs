// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class DictionaryExpressionTests : CSharpTestBase
    {
        private const string s_collectionExtensions = CollectionExpressionTests.s_collectionExtensions;
        private const string s_dictionaryExtensions = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Text;
            static class DictionaryExtensions
            {
                private static void Append(StringBuilder builder, object value)
                {
                    builder.Append(value is null ? "null" : value.ToString());
                }
                internal static void Report<K, V>(this IEnumerable<KeyValuePair<K, V>> e)
                {
                    e = e.OrderBy(kvp => kvp.Key);
                    var builder = new StringBuilder();
                    builder.Append("[");
                    bool isFirst = true;
                    foreach (var kvp in e)
                    {
                        if (!isFirst) builder.Append(", ");
                        isFirst = false;
                        Append(builder, kvp.Key);
                        builder.Append(":");
                        Append(builder, kvp.Value);
                    }
                    builder.Append("], ");
                    Console.Write(builder.ToString());
                }
            }
            """;

        public static readonly TheoryData<LanguageVersion> LanguageVersions = new([LanguageVersion.CSharp13, LanguageVersion.Preview, LanguageVersionFacts.CSharpNext]);

        [Theory]
        [MemberData(nameof(LanguageVersions))]
        public void LanguageVersionDiagnostics_01(LanguageVersion languageVersion)
        {
            string source = """
                using System.Collections.Generic;
                IDictionary<int, string> d = [1:"one"];
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (2,30): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // IDictionary<int, string> d = [1:"one"];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, @"[1:""one""]").WithArguments("dictionary expressions").WithLocation(2, 30),
                    // (2,32): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // IDictionary<int, string> d = [1:"one"];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, ":").WithArguments("dictionary expressions").WithLocation(2, 32));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [MemberData(nameof(LanguageVersions))]
        public void LanguageVersionDiagnostics_02(LanguageVersion languageVersion)
        {
            string source = """
                using System.Collections.Generic;
                IDictionary<int, string> d;
                d = [];
                var x = new KeyValuePair<int, string>(2, "two");
                var y = new KeyValuePair<int, string>[] { new(3, "three") };
                d = [x];
                d = [..y];
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (3,5): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // d = [];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "[]").WithArguments("dictionary expressions").WithLocation(3, 5),
                    // (6,5): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // d = [x];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "[x]").WithArguments("dictionary expressions").WithLocation(6, 5),
                    // (7,5): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // d = [..y];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "[..y]").WithArguments("dictionary expressions").WithLocation(7, 5));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [MemberData(nameof(LanguageVersions))]
        public void LanguageVersionDiagnostics_03(LanguageVersion languageVersion)
        {
            string source = """
                var x = [1:"one"];
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (1,9): error CS9176: There is no target type for the collection expression.
                    // var x = [1:"one"];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, @"[1:""one""]").WithLocation(1, 9),
                    // (1,11): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // var x = [1:"one"];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, ":").WithArguments("dictionary expressions").WithLocation(1, 11));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (1,9): error CS9176: There is no target type for the collection expression.
                    // var x = [1:"one"];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, @"[1:""one""]").WithLocation(1, 9));
            }
        }

        [Theory]
        [MemberData(nameof(LanguageVersions))]
        public void LanguageVersionDiagnostics_04(LanguageVersion languageVersion)
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Dictionary<int, string> d = [];
                        d.Report();
                    }
                }
                """;
            // C#12 collection expressions support target types that implement IEnumerable,
            // with no Add requirement if the collection is empty.
            var verifier = CompileAndVerify(
                [source, s_dictionaryExtensions],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                expectedOutput: "[], ");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.Main", """
                {
                  // Code size       11 (0xb)
                  .maxstack  1
                  IL_0000:  newobj     "System.Collections.Generic.Dictionary<int, string>..ctor()"
                  IL_0005:  call       "void DictionaryExtensions.Report<int, string>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>)"
                  IL_000a:  ret
                }
                """);
        }

        [Theory]
        [CombinatorialData]
        public void LanguageVersionDiagnostics_05(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion,
            bool includeExtensionAdd)
        {
            string sourceA = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Dictionary<int, string> d = [1:"one"];
                        d.Report();
                    }
                }
                """;
            string sourceB = """
                using System.Collections.Generic;
                static class Extensions
                {
                    internal static void Add<K, V>(this Dictionary<K, V> d, KeyValuePair<K, V> kvp)
                    {
                        d.Add(kvp.Key, kvp.Value);
                    }
                }
                """;
            var comp = CreateCompilation(
                includeExtensionAdd ? [sourceA, sourceB, s_dictionaryExtensions] : [sourceA, s_dictionaryExtensions],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13 && !includeExtensionAdd)
            {
                comp.VerifyEmitDiagnostics(
                    // (6,37): error CS9215: Collection expression type 'Dictionary<int, string>' must have an instance or extension method 'Add' that can be called with a single argument.
                    //         Dictionary<int, string> d = [1:"one"];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionMissingAdd, @"[1:""one""]").WithArguments("System.Collections.Generic.Dictionary<int, string>").WithLocation(6, 37),
                    // (6,38): error CS9268: Collection expression type 'Dictionary<int, string>' does not support key-value pair elements.
                    //         Dictionary<int, string> d = [1:"one"];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionKeyValuePairNotSupported, @"1:""one""").WithArguments("System.Collections.Generic.Dictionary<int, string>").WithLocation(6, 38),
                    // (6,39): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         Dictionary<int, string> d = [1:"one"];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, ":").WithArguments("dictionary expressions").WithLocation(6, 39));
            }
            else if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (6,38): error CS9268: Collection expression type 'Dictionary<int, string>' does not support key-value pair elements.
                    //         Dictionary<int, string> d = [1:"one"];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionKeyValuePairNotSupported, @"1:""one""").WithArguments("System.Collections.Generic.Dictionary<int, string>").WithLocation(6, 38),
                    // (6,39): error CS8652: The feature 'dictionary expressions' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         Dictionary<int, string> d = [1:"one"];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, ":").WithArguments("dictionary expressions").WithLocation(6, 39));
            }
            else
            {
                var verifier = CompileAndVerify(comp, expectedOutput: "[1:one], ");
                verifier.VerifyDiagnostics();
                verifier.VerifyIL("Program.Main", """
                    {
                      // Code size       23 (0x17)
                      .maxstack  4
                      IL_0000:  newobj     "System.Collections.Generic.Dictionary<int, string>..ctor()"
                      IL_0005:  dup
                      IL_0006:  ldc.i4.1
                      IL_0007:  ldstr      "one"
                      IL_000c:  callvirt   "void System.Collections.Generic.Dictionary<int, string>.this[int].set"
                      IL_0011:  call       "void DictionaryExtensions.Report<int, string>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>)"
                      IL_0016:  ret
                    }
                    """);
            }
        }

        // PROTOTYPE: Test with indexer with optional parameter, with extra params parameter, with params K[] keys.
        // PROTOTYPE: Test ref safety analysis of indexer set calls for the various cases in [e, k:v, ..s].
        // PROTOTYPE: Test interceptor targeting indexer setter.

        // PROTOTYPE: Test [x] and [..y] when the KVP does not match the iteration type exactly.
        // PROTOTYPE: Test [x] and [..y] when the element type is T, where T : KVP<K, V>.
        [Theory]
        [CombinatorialData]
        public void LanguageVersionDiagnostics_06(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion,
            bool includeExtensionAdd)
        {
            string sourceA = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Dictionary<int, string> d;
                        var x = new KeyValuePair<int, string>(2, "two");
                        var y = new KeyValuePair<int, string>[] { new(3, "three") };
                        d = [x];
                        d.Report();
                        d = [..y];
                        d.Report();
                    }
                }
                """;
            string sourceB = """
                using System.Collections.Generic;
                static class Extensions
                {
                    internal static void Add<K, V>(this Dictionary<K, V> d, KeyValuePair<K, V> kvp)
                    {
                        d.Add(kvp.Key, kvp.Value);
                    }
                }
                """;
            var comp = CreateCompilation(
                includeExtensionAdd ? [sourceA, sourceB, s_dictionaryExtensions] : [sourceA, s_dictionaryExtensions],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13 && !includeExtensionAdd)
            {
                comp.VerifyEmitDiagnostics(
                    // (9,13): error CS9215: Collection expression type 'Dictionary<int, string>' must have an instance or extension method 'Add' that can be called with a single argument.
                    //         d = [x];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionMissingAdd, "[x]").WithArguments("System.Collections.Generic.Dictionary<int, string>").WithLocation(9, 13),
                    // (11,13): error CS9215: Collection expression type 'Dictionary<int, string>' must have an instance or extension method 'Add' that can be called with a single argument.
                    //         d = [..y];
                    Diagnostic(ErrorCode.ERR_CollectionExpressionMissingAdd, "[..y]").WithArguments("System.Collections.Generic.Dictionary<int, string>").WithLocation(11, 13));
                return;
            }
            var verifier = CompileAndVerify(comp, expectedOutput: "[2:two], [3:three], ");
            verifier.VerifyDiagnostics();
            if (languageVersion == LanguageVersion.CSharp13)
            {
                verifier.VerifyIL("Program.Main", """
                    {
                        // Code size       99 (0x63)
                        .maxstack  5
                        .locals init (System.Collections.Generic.KeyValuePair<int, string> V_0, //x
                                    System.Collections.Generic.Dictionary<int, string> V_1,
                                    System.Collections.Generic.KeyValuePair<int, string>[] V_2,
                                    int V_3,
                                    System.Collections.Generic.KeyValuePair<int, string> V_4)
                        IL_0000:  ldloca.s   V_0
                        IL_0002:  ldc.i4.2
                        IL_0003:  ldstr      "two"
                        IL_0008:  call       "System.Collections.Generic.KeyValuePair<int, string>..ctor(int, string)"
                        IL_000d:  ldc.i4.1
                        IL_000e:  newarr     "System.Collections.Generic.KeyValuePair<int, string>"
                        IL_0013:  dup
                        IL_0014:  ldc.i4.0
                        IL_0015:  ldc.i4.3
                        IL_0016:  ldstr      "three"
                        IL_001b:  newobj     "System.Collections.Generic.KeyValuePair<int, string>..ctor(int, string)"
                        IL_0020:  stelem     "System.Collections.Generic.KeyValuePair<int, string>"
                        IL_0025:  newobj     "System.Collections.Generic.Dictionary<int, string>..ctor()"
                        IL_002a:  dup
                        IL_002b:  ldloc.0
                        IL_002c:  call       "void Extensions.Add<int, string>(System.Collections.Generic.Dictionary<int, string>, System.Collections.Generic.KeyValuePair<int, string>)"
                        IL_0031:  call       "void DictionaryExtensions.Report<int, string>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>)"
                        IL_0036:  newobj     "System.Collections.Generic.Dictionary<int, string>..ctor()"
                        IL_003b:  stloc.1
                        IL_003c:  stloc.2
                        IL_003d:  ldc.i4.0
                        IL_003e:  stloc.3
                        IL_003f:  br.s       IL_0056
                        IL_0041:  ldloc.2
                        IL_0042:  ldloc.3
                        IL_0043:  ldelem     "System.Collections.Generic.KeyValuePair<int, string>"
                        IL_0048:  stloc.s    V_4
                        IL_004a:  ldloc.1
                        IL_004b:  ldloc.s    V_4
                        IL_004d:  call       "void Extensions.Add<int, string>(System.Collections.Generic.Dictionary<int, string>, System.Collections.Generic.KeyValuePair<int, string>)"
                        IL_0052:  ldloc.3
                        IL_0053:  ldc.i4.1
                        IL_0054:  add
                        IL_0055:  stloc.3
                        IL_0056:  ldloc.3
                        IL_0057:  ldloc.2
                        IL_0058:  ldlen
                        IL_0059:  conv.i4
                        IL_005a:  blt.s      IL_0041
                        IL_005c:  ldloc.1
                        IL_005d:  call       "void DictionaryExtensions.Report<int, string>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>)"
                        IL_0062:  ret
                    }
                    """);
            }
            else
            {
                verifier.VerifyIL("Program.Main", """
                    {
                      // Code size      130 (0x82)
                      .maxstack  5
                      .locals init (System.Collections.Generic.KeyValuePair<int, string> V_0, //x
                                    System.Collections.Generic.KeyValuePair<int, string> V_1,
                                    System.Collections.Generic.Dictionary<int, string> V_2,
                                    System.Collections.Generic.KeyValuePair<int, string>[] V_3,
                                    int V_4)
                      IL_0000:  ldloca.s   V_0
                      IL_0002:  ldc.i4.2
                      IL_0003:  ldstr      "two"
                      IL_0008:  call       "System.Collections.Generic.KeyValuePair<int, string>..ctor(int, string)"
                      IL_000d:  ldc.i4.1
                      IL_000e:  newarr     "System.Collections.Generic.KeyValuePair<int, string>"
                      IL_0013:  dup
                      IL_0014:  ldc.i4.0
                      IL_0015:  ldc.i4.3
                      IL_0016:  ldstr      "three"
                      IL_001b:  newobj     "System.Collections.Generic.KeyValuePair<int, string>..ctor(int, string)"
                      IL_0020:  stelem     "System.Collections.Generic.KeyValuePair<int, string>"
                      IL_0025:  newobj     "System.Collections.Generic.Dictionary<int, string>..ctor()"
                      IL_002a:  ldloc.0
                      IL_002b:  stloc.1
                      IL_002c:  dup
                      IL_002d:  ldloca.s   V_1
                      IL_002f:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                      IL_0034:  ldloca.s   V_1
                      IL_0036:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                      IL_003b:  callvirt   "void System.Collections.Generic.Dictionary<int, string>.this[int].set"
                      IL_0040:  call       "void DictionaryExtensions.Report<int, string>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>)"
                      IL_0045:  newobj     "System.Collections.Generic.Dictionary<int, string>..ctor()"
                      IL_004a:  stloc.2
                      IL_004b:  stloc.3
                      IL_004c:  ldc.i4.0
                      IL_004d:  stloc.s    V_4
                      IL_004f:  br.s       IL_0074
                      IL_0051:  ldloc.3
                      IL_0052:  ldloc.s    V_4
                      IL_0054:  ldelem     "System.Collections.Generic.KeyValuePair<int, string>"
                      IL_0059:  stloc.1
                      IL_005a:  ldloc.2
                      IL_005b:  ldloca.s   V_1
                      IL_005d:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                      IL_0062:  ldloca.s   V_1
                      IL_0064:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                      IL_0069:  callvirt   "void System.Collections.Generic.Dictionary<int, string>.this[int].set"
                      IL_006e:  ldloc.s    V_4
                      IL_0070:  ldc.i4.1
                      IL_0071:  add
                      IL_0072:  stloc.s    V_4
                      IL_0074:  ldloc.s    V_4
                      IL_0076:  ldloc.3
                      IL_0077:  ldlen
                      IL_0078:  conv.i4
                      IL_0079:  blt.s      IL_0051
                      IL_007b:  ldloc.2
                      IL_007c:  call       "void DictionaryExtensions.Report<int, string>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>)"
                      IL_0081:  ret
                    }
                    """);
            }
        }

        [Theory]
        [InlineData("Dictionary")]
        [InlineData("IDictionary")]
        [InlineData("IReadOnlyDictionary")]
        public void Dictionary_01(string typeName)
        {
            string source = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        F<int, string>().Report();
                    }
                    static {{typeName}}<K, V> F<K, V>()
                    {
                        return [];
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_dictionaryExtensions],
                expectedOutput: "[], ");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.F<K, V>", """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor()"
                  IL_0005:  ret
                }
                """);
        }

        // PROTOTYPE: Test async values: d = [await k : await v];

        [Theory]
        [InlineData("Dictionary")]
        [InlineData("IDictionary")]
        [InlineData("IReadOnlyDictionary")]
        public void Dictionary_02(string typeName)
        {
            string source = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(2, "two");
                        var y = new KeyValuePair<int, string>[] { new(3, "three") };
                        F(1, "one", x, y).Report();
                    }
                    static {{typeName}}<K, V> F<K, V>(K k, V v, KeyValuePair<K, V> e, IEnumerable<KeyValuePair<K, V>> s)
                    {
                        return /*<bind>*/[k:v, e, ..s]/*</bind>*/;
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_dictionaryExtensions],
                expectedOutput: "[1:one, 2:two, 3:three], ");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.F<K, V>", """
                {
                  // Code size       94 (0x5e)
                  .maxstack  3
                  .locals init (System.Collections.Generic.Dictionary<K, V> V_0,
                                System.Collections.Generic.KeyValuePair<K, V> V_1,
                                System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<K, V>> V_2,
                                System.Collections.Generic.KeyValuePair<K, V> V_3)
                  IL_0000:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldloc.0
                  IL_0007:  ldarg.0
                  IL_0008:  ldarg.1
                  IL_0009:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                  IL_000e:  ldarg.2
                  IL_000f:  stloc.1
                  IL_0010:  ldloc.0
                  IL_0011:  ldloca.s   V_1
                  IL_0013:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                  IL_0018:  ldloca.s   V_1
                  IL_001a:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                  IL_001f:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                  IL_0024:  ldarg.3
                  IL_0025:  callvirt   "System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<K, V>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<K, V>>.GetEnumerator()"
                  IL_002a:  stloc.2
                  .try
                  {
                    IL_002b:  br.s       IL_0048
                    IL_002d:  ldloc.2
                    IL_002e:  callvirt   "System.Collections.Generic.KeyValuePair<K, V> System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<K, V>>.Current.get"
                    IL_0033:  stloc.3
                    IL_0034:  ldloc.0
                    IL_0035:  ldloca.s   V_3
                    IL_0037:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                    IL_003c:  ldloca.s   V_3
                    IL_003e:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                    IL_0043:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                    IL_0048:  ldloc.2
                    IL_0049:  callvirt   "bool System.Collections.IEnumerator.MoveNext()"
                    IL_004e:  brtrue.s   IL_002d
                    IL_0050:  leave.s    IL_005c
                  }
                  finally
                  {
                    IL_0052:  ldloc.2
                    IL_0053:  brfalse.s  IL_005b
                    IL_0055:  ldloc.2
                    IL_0056:  callvirt   "void System.IDisposable.Dispose()"
                    IL_005b:  endfinally
                  }
                  IL_005c:  ldloc.0
                  IL_005d:  ret
                }
                """);
            var comp = (CSharpCompilation)verifier.Compilation;
            string constructMethod = typeName == "Dictionary" ? "System.Collections.Generic.Dictionary<K, V>..ctor()" : "null";
            VerifyOperationTreeForTest<CollectionExpressionSyntax>(comp,
                $$"""
                ICollectionExpressionOperation (3 elements, ConstructMethod: {{constructMethod}}) (OperationKind.CollectionExpression, Type: System.Collections.Generic.{{typeName}}<K, V>) (Syntax: '[k:v, e, ..s]')
                  Elements(3):
                      IOperation:  (OperationKind.None, Type: null) (Syntax: 'k:v')
                      IOperation:  (OperationKind.None, Type: null) (Syntax: 'e')
                      ISpreadOperation (ElementType: System.Collections.Generic.KeyValuePair<K, V>) (OperationKind.Spread, Type: null) (Syntax: '..s')
                        Operand:
                          IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<K, V>>) (Syntax: 's')
                        ElementConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          (Identity)
                """);
        }

        [Theory]
        [InlineData("Dictionary")]
        [InlineData("IDictionary")]
        [InlineData("IReadOnlyDictionary")]
        public void Dictionary_DuplicateKeys(string typeName)
        {
            string source = $$"""
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(101, "one");
                        var y = new KeyValuePair<int, string>(202, "two");
                        var z = new KeyValuePair<int, string>(101, "three");
                        Report(F1(x, y, z));
                        Report(F1(y, z, x));
                        Report(F1(z, x, y));
                        Report(F2(x, y, z));
                        Report(F2(y, z, x));
                        Report(F2(z, x, y));
                        Report(F3(x, y, z));
                        Report(F3(y, z, x));
                        Report(F3(z, x, y));
                    }
                    static void Report<K, V>({{typeName}}<K, V> d)
                    {
                        d.Report();
                        Console.WriteLine();
                    }
                    static {{typeName}}<K, V> F1<K, V>(KeyValuePair<K, V> x, KeyValuePair<K, V> y, KeyValuePair<K, V> z)
                    {
                        return [x.Key:x.Value, y, .. new[] { z }];
                    }
                    static {{typeName}}<K, V> F2<K, V>(KeyValuePair<K, V> x, KeyValuePair<K, V> y, KeyValuePair<K, V> z)
                    {
                        return [x, .. new[] { y }, z.Key:z.Value];
                    }
                    static {{typeName}}<K, V> F3<K, V>(KeyValuePair<K, V> x, KeyValuePair<K, V> y, KeyValuePair<K, V> z)
                    {
                        return [.. new[] { x }, y.Key:y.Value, z];
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_dictionaryExtensions],
                expectedOutput: """
                    [101:three, 202:two], 
                    [101:one, 202:two], 
                    [101:one, 202:two], 
                    [101:three, 202:two], 
                    [101:one, 202:two], 
                    [101:one, 202:two], 
                    [101:three, 202:two], 
                    [101:one, 202:two], 
                    [101:one, 202:two], 
                    """);
            verifier.VerifyDiagnostics();
        }

        [Theory]
        [InlineData("Dictionary")]
        [InlineData("IDictionary")]
        [InlineData("IReadOnlyDictionary")]
        public void DictionaryNotImplementingIDictionary(string typeName)
        {
            string sourceA = """
                namespace System
                {
                    public class Object { }
                    public abstract class ValueType { }
                    public class String { }
                    public class Type { }
                    public struct Void { }
                    public struct Boolean { }
                    public struct Int32 { }
                    public struct Enum { }
                    public class Attribute { }
                    public class AttributeUsageAttribute : Attribute
                    {
                        public AttributeUsageAttribute(AttributeTargets t) { }
                        public bool AllowMultiple { get; set; }
                        public bool Inherited { get; set; }
                    }
                    public enum AttributeTargets { }
                    public interface IDisposable
                    {
                        void Dispose();
                    }
                }
                namespace System.Collections
                {
                    public interface IEnumerator
                    {
                        bool MoveNext();
                        object Current { get; }
                    }
                    public interface IEnumerable
                    {
                        IEnumerator GetEnumerator();
                    }
                }
                namespace System.Collections.Generic
                {
                    public interface IEnumerator<T> : IEnumerator
                    {
                        new T Current { get; }
                    }
                    public interface IEnumerable<T> : IEnumerable
                    {
                        new IEnumerator<T> GetEnumerator();
                    }
                    public interface IDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
                    {
                    }
                    public interface IReadOnlyDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
                    {
                    }
                    public struct KeyValuePair<TKey, TValue>
                    {
                        public TKey Key { get; }
                        public TValue Value { get; }
                    }
                    public sealed class Dictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
                    {
                        public Dictionary() { }
                        public TValue this[TKey key] { get { return default; } set { } }
                        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => null;
                        IEnumerator IEnumerable.GetEnumerator() => null;
                    }
                }
                namespace System.Reflection
                {
                    public class DefaultMemberAttribute : Attribute
                    {
                        public DefaultMemberAttribute(string name) { }
                    }
                }
                """;
            string sourceB = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var d = F(1, "one", new KeyValuePair<int, string>(), null);
                    }
                    static {{typeName}}<K, V> F<K, V>(K k, V v, KeyValuePair<K, V> x, IEnumerable<KeyValuePair<K, V>> y)
                    {
                        return [k:v, x, ..y];
                    }
                }
                """;
            var comp = CreateEmptyCompilation(new[] { sourceA, sourceB });
            var emitOptions = Microsoft.CodeAnalysis.Emit.EmitOptions.Default.WithRuntimeMetadataVersion("0.0.0.0");
            if (typeName == "Dictionary")
            {
                comp.VerifyEmitDiagnostics(emitOptions);
            }
            else
            {
                comp.VerifyEmitDiagnostics(emitOptions,
                    // 1.cs(10,16): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.Dictionary<K, V>' to 'System.Collections.Generic.IDictionary<K, V>'
                    //         return [k:v, x, ..y];
                    Diagnostic(ErrorCode.ERR_NoImplicitConv, "[k:v, x, ..y]").WithArguments("System.Collections.Generic.Dictionary<K, V>", $"System.Collections.Generic.{typeName}<K, V>").WithLocation(10, 16));
            }
        }

        [Theory]
        [InlineData("IDictionary<int, string>")]
        [InlineData("IReadOnlyDictionary<int, string>")]
        public void DictionaryInterface_MissingMember(string typeName)
        {
            string source = $$"""
                using System.Collections.Generic;
                {{typeName}} d;
                d = [];
                d = [1:"one"];
                d = [new KeyValuePair<int, string>(2, "two")];
                d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                """;

            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Collections_Generic_List_T);
            comp.VerifyEmitDiagnostics();

            comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Collections_Generic_Dictionary_KV);
            comp.VerifyEmitDiagnostics(
                // (3,5): error CS0518: Predefined type 'System.Collections.Generic.Dictionary`2' is not defined or imported
                // d = [];
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "[]").WithArguments("System.Collections.Generic.Dictionary`2").WithLocation(3, 5),
                // (3,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                // d = [];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(3, 5),
                // (3,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2.set_Item'
                // d = [];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.Dictionary`2", "set_Item").WithLocation(3, 5),
                // (4,5): error CS0518: Predefined type 'System.Collections.Generic.Dictionary`2' is not defined or imported
                // d = [1:"one"];
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"[1:""one""]").WithArguments("System.Collections.Generic.Dictionary`2").WithLocation(4, 5),
                // (4,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                // d = [1:"one"];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[1:""one""]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(4, 5),
                // (4,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2.set_Item'
                // d = [1:"one"];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[1:""one""]").WithArguments("System.Collections.Generic.Dictionary`2", "set_Item").WithLocation(4, 5),
                // (5,5): error CS0518: Predefined type 'System.Collections.Generic.Dictionary`2' is not defined or imported
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"[new KeyValuePair<int, string>(2, ""two"")]").WithArguments("System.Collections.Generic.Dictionary`2").WithLocation(5, 5),
                // (5,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[new KeyValuePair<int, string>(2, ""two"")]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(5, 5),
                // (5,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2.set_Item'
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[new KeyValuePair<int, string>(2, ""two"")]").WithArguments("System.Collections.Generic.Dictionary`2", "set_Item").WithLocation(5, 5),
                // (6,5): error CS0518: Predefined type 'System.Collections.Generic.Dictionary`2' is not defined or imported
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"[.. new KeyValuePair<int, string>[] { new(3, ""three"") }]").WithArguments("System.Collections.Generic.Dictionary`2").WithLocation(6, 5),
                // (6,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[.. new KeyValuePair<int, string>[] { new(3, ""three"") }]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(6, 5),
                // (6,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2.set_Item'
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[.. new KeyValuePair<int, string>[] { new(3, ""three"") }]").WithArguments("System.Collections.Generic.Dictionary`2", "set_Item").WithLocation(6, 5));

            comp = CreateCompilation(source);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_Dictionary_KV__ctor);
            comp.VerifyEmitDiagnostics(
                // (3,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                // d = [];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(3, 5),
                // (4,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                // d = [1:"one"];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[1:""one""]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(4, 5),
                // (5,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[new KeyValuePair<int, string>(2, ""two"")]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(5, 5),
                // (6,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[.. new KeyValuePair<int, string>[] { new(3, ""three"") }]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(6, 5));

            comp = CreateCompilation(source);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_Dictionary_KV__set_Item);
            comp.VerifyEmitDiagnostics(
                // (3,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2.set_Item'
                // d = [];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.Dictionary`2", "set_Item").WithLocation(3, 5),
                // (4,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2.set_Item'
                // d = [1:"one"];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[1:""one""]").WithArguments("System.Collections.Generic.Dictionary`2", "set_Item").WithLocation(4, 5),
                // (5,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2.set_Item'
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[new KeyValuePair<int, string>(2, ""two"")]").WithArguments("System.Collections.Generic.Dictionary`2", "set_Item").WithLocation(5, 5),
                // (6,5): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2.set_Item'
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[.. new KeyValuePair<int, string>[] { new(3, ""three"") }]").WithArguments("System.Collections.Generic.Dictionary`2", "set_Item").WithLocation(6, 5));
        }

        [Theory]
        [InlineData("IDictionary<int, string>")]
        [InlineData("IReadOnlyDictionary<int, string>")]
        public void KeyValuePair_MissingMember_DictionaryInterface(string typeName)
        {
            string source = $$"""
                using System.Collections.Generic;
                {{typeName}} d;
                d = [];
                d = [1:"one"];
                d = [new KeyValuePair<int, string>(2, "two")];
                d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();

            comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Collections_Generic_KeyValuePair_KV);
            comp.VerifyEmitDiagnostics(
                // (3,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Key'
                // d = [];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Key").WithLocation(3, 5),
                // (3,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Value'
                // d = [];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Value").WithLocation(3, 5),
                // (4,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Key'
                // d = [1:"one"];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[1:""one""]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Key").WithLocation(4, 5),
                // (4,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Value'
                // d = [1:"one"];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[1:""one""]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Value").WithLocation(4, 5),
                // (5,6): error CS0029: Cannot implicitly convert type 'KeyValuePair<int, string>' to 'KeyValuePair<int, string>'
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"new KeyValuePair<int, string>(2, ""two"")").WithArguments("System.Collections.Generic.KeyValuePair<int, string>", "System.Collections.Generic.KeyValuePair<int, string>").WithLocation(5, 6),
                // (6,9): error CS0029: Cannot implicitly convert type 'KeyValuePair<int, string>' to 'KeyValuePair<int, string>'
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"new KeyValuePair<int, string>[] { new(3, ""three"") }").WithArguments("System.Collections.Generic.KeyValuePair<int, string>", "System.Collections.Generic.KeyValuePair<int, string>").WithLocation(6, 9));

            comp = CreateCompilation(source);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_KeyValuePair_KV__get_Key);
            comp.VerifyEmitDiagnostics(
                // (3,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Key'
                // d = [];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Key").WithLocation(3, 5),
                // (4,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Key'
                // d = [1:"one"];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[1:""one""]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Key").WithLocation(4, 5),
                // (5,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Key'
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[new KeyValuePair<int, string>(2, ""two"")]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Key").WithLocation(5, 5),
                // (6,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Key'
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[.. new KeyValuePair<int, string>[] { new(3, ""three"") }]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Key").WithLocation(6, 5));

            comp = CreateCompilation(source);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_KeyValuePair_KV__get_Value);
            comp.VerifyEmitDiagnostics(
                // (3,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Value'
                // d = [];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Value").WithLocation(3, 5),
                // (4,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Value'
                // d = [1:"one"];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[1:""one""]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Value").WithLocation(4, 5),
                // (5,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Value'
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[new KeyValuePair<int, string>(2, ""two"")]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Value").WithLocation(5, 5),
                // (6,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Value'
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[.. new KeyValuePair<int, string>[] { new(3, ""three"") }]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Value").WithLocation(6, 5));
        }

        // PROTOTYPE: Test with [CollectionBuilder] type as well. In particular, should allow all three of [k:v, e, ..s] where Ke, Ve do not match K, V exactly.
        [Fact]
        public void KeyValuePair_MissingMember_CustomDictionary()
        {
            string sourceA = """
                using System.Collections;
                using System.Collections.Generic;
                public class MyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    public V this[K k] { get => default; set { } }
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();

            string sourceB = """
                using System.Collections.Generic;
                MyDictionary<int, string> d;
                d = [];
                d = [1:"one"];
                d = [new KeyValuePair<int, string>(2, "two")];
                d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                """;

            comp = CreateCompilation(sourceB, references: [refA]);
            comp.VerifyEmitDiagnostics();

            comp = CreateCompilation(sourceB, references: [refA]);
            comp.MakeTypeMissing(WellKnownType.System_Collections_Generic_KeyValuePair_KV);
            comp.VerifyEmitDiagnostics(
                // (4,5): error CS1061: 'MyDictionary<int, string>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<int, string>' could be found (are you missing a using directive or an assembly reference?)
                // d = [1:"one"];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, @"[1:""one""]").WithArguments("MyDictionary<int, string>", "Add").WithLocation(4, 5),
                // (4,6): error CS9275: Collection expression type 'MyDictionary<int, string>' does not support key-value pair elements.
                // d = [1:"one"];
                Diagnostic(ErrorCode.ERR_CollectionExpressionKeyValuePairNotSupported, @"1:""one""").WithArguments("MyDictionary<int, string>").WithLocation(4, 6),
                // (5,5): error CS1061: 'MyDictionary<int, string>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<int, string>' could be found (are you missing a using directive or an assembly reference?)
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, @"[new KeyValuePair<int, string>(2, ""two"")]").WithArguments("MyDictionary<int, string>", "Add").WithLocation(5, 5),
                // (6,5): error CS1061: 'MyDictionary<int, string>' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'MyDictionary<int, string>' could be found (are you missing a using directive or an assembly reference?)
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, @"[.. new KeyValuePair<int, string>[] { new(3, ""three"") }]").WithArguments("MyDictionary<int, string>", "Add").WithLocation(6, 5));

            comp = CreateCompilation(sourceB, references: [refA]);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_KeyValuePair_KV__get_Key);
            comp.VerifyEmitDiagnostics(
                // (3,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Key'
                // d = [];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Key").WithLocation(3, 5),
                // (4,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Key'
                // d = [1:"one"];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[1:""one""]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Key").WithLocation(4, 5),
                // (5,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Key'
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[new KeyValuePair<int, string>(2, ""two"")]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Key").WithLocation(5, 5),
                // (6,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Key'
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[.. new KeyValuePair<int, string>[] { new(3, ""three"") }]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Key").WithLocation(6, 5));

            comp = CreateCompilation(sourceB, references: [refA]);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_KeyValuePair_KV__get_Value);
            comp.VerifyEmitDiagnostics(
                // (3,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Value'
                // d = [];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Value").WithLocation(3, 5),
                // (4,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Value'
                // d = [1:"one"];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[1:""one""]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Value").WithLocation(4, 5),
                // (5,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Value'
                // d = [new KeyValuePair<int, string>(2, "two")];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[new KeyValuePair<int, string>(2, ""two"")]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Value").WithLocation(5, 5),
                // (6,5): error CS0656: Missing compiler required member 'System.Collections.Generic.KeyValuePair`2.get_Value'
                // d = [.. new KeyValuePair<int, string>[] { new(3, "three") }];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"[.. new KeyValuePair<int, string>[] { new(3, ""three"") }]").WithArguments("System.Collections.Generic.KeyValuePair`2", "get_Value").WithLocation(6, 5));
        }

        [Fact]
        public void KeyValuePairConversions_01()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        F(1, "one").Report();
                    }
                    static IDictionary<long, object> F(int x, string y)
                    {
                        return /*<bind>*/[x:y]/*</bind>*/;
                    }
                }
                """;
            var comp = CreateCompilation([source, s_dictionaryExtensions], options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "[1:one], ");
            verifier.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var kvpElement = tree.GetRoot().DescendantNodes().OfType<KeyValuePairElementSyntax>().Single();
            var typeInfo = model.GetTypeInfo(kvpElement.KeyExpression);
            Assert.Equal(SpecialType.System_Int32, typeInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_Int64, typeInfo.ConvertedType.SpecialType);
            typeInfo = model.GetTypeInfo(kvpElement.ValueExpression);
            Assert.Equal(SpecialType.System_String, typeInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_Object, typeInfo.ConvertedType.SpecialType);

            // PROTOTYPE: Implement IOperation support.
            VerifyOperationTreeForTest<CollectionExpressionSyntax>(comp,
                """
                ICollectionExpressionOperation (1 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Collections.Generic.IDictionary<System.Int64, System.Object>) (Syntax: '[x:y]')
                  Elements(1):
                      IOperation:  (OperationKind.None, Type: null) (Syntax: 'x:y')
                """);
        }

        // PROTOTYPE: Do we actually want to support these conversions from KeyValuePair<K1, V1> to KeyValuePair<K2, V2>?
        [Fact]
        public void KeyValuePairConversions_02()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new KeyValuePair<int, string>(2, "two");
                        var y = new KeyValuePair<int, string>[] { new(3, "three") };
                        var d = F(x, y);
                        d.Report();
                    }
                    static IDictionary<long, object> F(KeyValuePair<int, string> x, IEnumerable<KeyValuePair<int, string>> y)
                    {
                        return [x, ..y];
                    }
                }
                """;
            var comp = CreateCompilation([source, s_dictionaryExtensions], options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "[2:two, 3:three], ");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void KeyValuePairConversions_03()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static IDictionary<int, string> F1(KeyValuePair<int?, string> x1, IEnumerable<KeyValuePair<int?, string>> y1)
                    {
                        return [x1, ..y1];
                    }
                    static IDictionary<int, string> F2(KeyValuePair<int, object> x2, IEnumerable<KeyValuePair<int, object>> y2)
                    {
                        return [x2, ..y2];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,17): error CS0029: Cannot implicitly convert type 'int?' to 'int'
                //         return [x1, ..y1];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x1").WithArguments("int?", "int").WithLocation(6, 17),
                // (6,23): error CS0029: Cannot implicitly convert type 'int?' to 'int'
                //         return [x1, ..y1];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "y1").WithArguments("int?", "int").WithLocation(6, 23),
                // (10,17): error CS0029: Cannot implicitly convert type 'object' to 'string'
                //         return [x2, ..y2];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x2").WithArguments("object", "string").WithLocation(10, 17),
                // (10,23): error CS0029: Cannot implicitly convert type 'object' to 'string'
                //         return [x2, ..y2];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "y2").WithArguments("object", "string").WithLocation(10, 23));
        }

        [Fact]
        public void KeyValuePairConversions_04()
        {
            string source = """
                using System.Collections.Generic;
                IDictionary<int, int> d;
                d = [null:default];
                d = [default:null];
                d = [default, null];
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,6): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                // d = [null:default];
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(3, 6),
                // (4,14): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                // d = [default:null];
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(4, 14),
                // (5,15): error CS0037: Cannot convert null to 'KeyValuePair<int, int>' because it is a non-nullable value type
                // d = [default, null];
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("System.Collections.Generic.KeyValuePair<int, int>").WithLocation(5, 15));
        }

        [Fact]
        public void KeyValuePairConversions_05()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static IDictionary<K, V> F1<K, V>(K k) => [k];
                    static IDictionary<K, V> F2<K, V>(IEnumerable<K> e) => [..e];
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,48): error CS0029: Cannot implicitly convert type 'K' to 'System.Collections.Generic.KeyValuePair<K, V>'
                //     static IDictionary<K, V> F1<K, V>(K k) => [k];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "k").WithArguments("K", "System.Collections.Generic.KeyValuePair<K, V>").WithLocation(4, 48),
                // (5,63): error CS0029: Cannot implicitly convert type 'K' to 'System.Collections.Generic.KeyValuePair<K, V>'
                //     static IDictionary<K, V> F2<K, V>(IEnumerable<K> e) => [..e];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "e").WithArguments("K", "System.Collections.Generic.KeyValuePair<K, V>").WithLocation(5, 63));
        }

        [Fact]
        public void KeyValuePairConversions_06()
        {
            string sourceA = """
                using System.Collections.Generic;
                public class MyKeyValuePair<K, V>
                {
                    public MyKeyValuePair(K key, V value)
                    {
                        Key = key;
                        Value = value;
                    }
                    public readonly K Key;
                    public readonly V Value;
                    public override string ToString() => $"{Key}:{Value}";
                    public static implicit operator MyKeyValuePair<K, V>(KeyValuePair<K, V> kvp) => new(kvp.Key, kvp.Value);
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();

            string sourceB1 = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        F(1, "one");
                    }
                    static IEnumerable<MyKeyValuePair<K, V>> F<K, V>(K k, V v)
                    {
                        return [k:v];
                    }
                }
                """;
            comp = CreateCompilation(sourceB1, references: [refA]);
            comp.VerifyEmitDiagnostics(
                // (10,17): error CS9275: Collection expression type 'IEnumerable<MyKeyValuePair<K, V>>' does not support key-value pair elements.
                //         return [k:v];
                Diagnostic(ErrorCode.ERR_CollectionExpressionKeyValuePairNotSupported, "k:v").WithArguments("System.Collections.Generic.IEnumerable<MyKeyValuePair<K, V>>").WithLocation(10, 17));

            string sourceB2 = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var e = F(new KeyValuePair<int, string>(2, "two"), new KeyValuePair<int, string>[] { new(3, "three") });
                        e.Report();
                    }
                    static IEnumerable<MyKeyValuePair<K, V>> F<K, V>(KeyValuePair<K, V> x, IEnumerable<KeyValuePair<K, V>> y)
                    {
                        return [x, ..y];
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [sourceB2, s_collectionExtensions],
                references: [refA],
                expectedOutput: "[2:two, 3:three], ");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void KeyValuePairConversions_07()
        {
            string sourceA = """
                using System.Collections.Generic;
                public class MyKeyValuePair<K, V>
                {
                    public MyKeyValuePair(K key, V value)
                    {
                        Key = key;
                        Value = value;
                    }
                    public readonly K Key;
                    public readonly V Value;
                    public static implicit operator KeyValuePair<K, V>(MyKeyValuePair<K, V> kvp) => new(kvp.Key, kvp.Value);
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();

            string sourceB = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var x = new MyKeyValuePair<int, string>(2, "two");
                        var y = new MyKeyValuePair<int, string>[] { new(3, "three") };
                        F1(x, y);
                        F2(x, y);
                    }
                    static IEnumerable<KeyValuePair<K, V>> F1<K, V>(MyKeyValuePair<K, V> x, IEnumerable<MyKeyValuePair<K, V>> y)
                    {
                        return [x, ..y];
                    }
                    static IDictionary<K, V> F2<K, V>(MyKeyValuePair<K, V> x, IEnumerable<MyKeyValuePair<K, V>> y)
                    {
                        return [x, ..y];
                    }
                }
                """;
            comp = CreateCompilation(sourceB, references: [refA]);
            comp.VerifyEmitDiagnostics(
                // (17,17): error CS0029: Cannot implicitly convert type 'MyKeyValuePair<K, V>' to 'KeyValuePair<K, V>'
                //         return [x, ..y];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("MyKeyValuePair<K, V>", "System.Collections.Generic.KeyValuePair<K, V>").WithLocation(17, 17),
                // (17,22): error CS0029: Cannot implicitly convert type 'MyKeyValuePair<K, V>' to 'KeyValuePair<K, V>'
                //         return [x, ..y];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "y").WithArguments("MyKeyValuePair<K, V>", "System.Collections.Generic.KeyValuePair<K, V>").WithLocation(17, 22));
        }

        [Fact]
        public void EvaluationOrder_01()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        IReadOnlyDictionary<int, string> d = [
                            Identity(new KeyValuePair<int, string>(1, "one")),
                            Identity(2):Identity("two"),
                            ..Identity((KeyValuePair<int, string>[])[new(3, "three")])];
                        d.Report();
                    }
                    static T Identity<T>(T value)
                    {
                        Console.WriteLine(value);
                        return value;
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_dictionaryExtensions],
                expectedOutput: """
                    [1, one]
                    2
                    two
                    System.Collections.Generic.KeyValuePair`2[System.Int32,System.String][]
                    [1:one, 2:two, 3:three], 
                    """);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.Main", """
                {
                  // Code size      145 (0x91)
                  .maxstack  5
                  .locals init (System.Collections.Generic.Dictionary<int, string> V_0,
                                System.Collections.Generic.KeyValuePair<int, string> V_1,
                                System.Collections.Generic.KeyValuePair<int, string>[] V_2,
                                int V_3,
                                System.Collections.Generic.KeyValuePair<int, string> V_4)
                  IL_0000:  newobj     "System.Collections.Generic.Dictionary<int, string>..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldc.i4.1
                  IL_0007:  ldstr      "one"
                  IL_000c:  newobj     "System.Collections.Generic.KeyValuePair<int, string>..ctor(int, string)"
                  IL_0011:  call       "System.Collections.Generic.KeyValuePair<int, string> Program.Identity<System.Collections.Generic.KeyValuePair<int, string>>(System.Collections.Generic.KeyValuePair<int, string>)"
                  IL_0016:  stloc.1
                  IL_0017:  ldloc.0
                  IL_0018:  ldloca.s   V_1
                  IL_001a:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                  IL_001f:  ldloca.s   V_1
                  IL_0021:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                  IL_0026:  callvirt   "void System.Collections.Generic.Dictionary<int, string>.this[int].set"
                  IL_002b:  ldloc.0
                  IL_002c:  ldc.i4.2
                  IL_002d:  call       "int Program.Identity<int>(int)"
                  IL_0032:  ldstr      "two"
                  IL_0037:  call       "string Program.Identity<string>(string)"
                  IL_003c:  callvirt   "void System.Collections.Generic.Dictionary<int, string>.this[int].set"
                  IL_0041:  ldc.i4.1
                  IL_0042:  newarr     "System.Collections.Generic.KeyValuePair<int, string>"
                  IL_0047:  dup
                  IL_0048:  ldc.i4.0
                  IL_0049:  ldc.i4.3
                  IL_004a:  ldstr      "three"
                  IL_004f:  newobj     "System.Collections.Generic.KeyValuePair<int, string>..ctor(int, string)"
                  IL_0054:  stelem     "System.Collections.Generic.KeyValuePair<int, string>"
                  IL_0059:  call       "System.Collections.Generic.KeyValuePair<int, string>[] Program.Identity<System.Collections.Generic.KeyValuePair<int, string>[]>(System.Collections.Generic.KeyValuePair<int, string>[])"
                  IL_005e:  stloc.2
                  IL_005f:  ldc.i4.0
                  IL_0060:  stloc.3
                  IL_0061:  br.s       IL_0084
                  IL_0063:  ldloc.2
                  IL_0064:  ldloc.3
                  IL_0065:  ldelem     "System.Collections.Generic.KeyValuePair<int, string>"
                  IL_006a:  stloc.s    V_4
                  IL_006c:  ldloc.0
                  IL_006d:  ldloca.s   V_4
                  IL_006f:  call       "int System.Collections.Generic.KeyValuePair<int, string>.Key.get"
                  IL_0074:  ldloca.s   V_4
                  IL_0076:  call       "string System.Collections.Generic.KeyValuePair<int, string>.Value.get"
                  IL_007b:  callvirt   "void System.Collections.Generic.Dictionary<int, string>.this[int].set"
                  IL_0080:  ldloc.3
                  IL_0081:  ldc.i4.1
                  IL_0082:  add
                  IL_0083:  stloc.3
                  IL_0084:  ldloc.3
                  IL_0085:  ldloc.2
                  IL_0086:  ldlen
                  IL_0087:  conv.i4
                  IL_0088:  blt.s      IL_0063
                  IL_008a:  ldloc.0
                  IL_008b:  call       "void DictionaryExtensions.Report<int, string>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int, string>>)"
                  IL_0090:  ret
                }
                """);
        }

        [Fact]
        public void EvaluationOrder_02()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var d = F(101, "A", 102, "B", 103, "C", true, 3);
                        d.Report();
                    }
                    static IReadOnlyDictionary<K, V> F<K, V>(K k1, V v1, K k2, V v2, K k3, V v3, bool b, int i)
                    {
                        return [
                            Identity(Identity(b) ? Identity(k1) : Identity(k2)) : Identity(v2),
                            Identity(k3) : Identity(Identity(i) switch { 1 => Identity(v1), 2 => Identity(v2), _ => Identity(v3) })];
                    }
                    static T Identity<T>(T value)
                    {
                        Console.WriteLine(value);
                        return value;
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_dictionaryExtensions],
                expectedOutput: """
                    True
                    101
                    101
                    B
                    103
                    3
                    C
                    C
                    [101:B, 103:C], 
                    """);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.F<K, V>", """
                {
                  // Code size      119 (0x77)
                  .maxstack  3
                  .locals init (System.Collections.Generic.Dictionary<K, V> V_0,
                                K V_1,
                                V V_2,
                                int V_3,
                                System.Collections.Generic.Dictionary<K, V> V_4)
                  IL_0000:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor()"
                  IL_0005:  stloc.s    V_4
                  IL_0007:  ldloc.s    V_4
                  IL_0009:  ldarg.s    V_6
                  IL_000b:  call       "bool Program.Identity<bool>(bool)"
                  IL_0010:  brtrue.s   IL_001a
                  IL_0012:  ldarg.2
                  IL_0013:  call       "K Program.Identity<K>(K)"
                  IL_0018:  br.s       IL_0020
                  IL_001a:  ldarg.0
                  IL_001b:  call       "K Program.Identity<K>(K)"
                  IL_0020:  call       "K Program.Identity<K>(K)"
                  IL_0025:  ldarg.3
                  IL_0026:  call       "V Program.Identity<V>(V)"
                  IL_002b:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                  IL_0030:  ldloc.s    V_4
                  IL_0032:  stloc.0
                  IL_0033:  ldarg.s    V_4
                  IL_0035:  call       "K Program.Identity<K>(K)"
                  IL_003a:  stloc.1
                  IL_003b:  ldarg.s    V_7
                  IL_003d:  call       "int Program.Identity<int>(int)"
                  IL_0042:  stloc.3
                  IL_0043:  ldloc.3
                  IL_0044:  ldc.i4.1
                  IL_0045:  beq.s      IL_004d
                  IL_0047:  ldloc.3
                  IL_0048:  ldc.i4.2
                  IL_0049:  beq.s      IL_0056
                  IL_004b:  br.s       IL_005f
                  IL_004d:  ldarg.1
                  IL_004e:  call       "V Program.Identity<V>(V)"
                  IL_0053:  stloc.2
                  IL_0054:  br.s       IL_0067
                  IL_0056:  ldarg.3
                  IL_0057:  call       "V Program.Identity<V>(V)"
                  IL_005c:  stloc.2
                  IL_005d:  br.s       IL_0067
                  IL_005f:  ldarg.s    V_5
                  IL_0061:  call       "V Program.Identity<V>(V)"
                  IL_0066:  stloc.2
                  IL_0067:  ldloc.0
                  IL_0068:  ldloc.1
                  IL_0069:  ldloc.2
                  IL_006a:  call       "V Program.Identity<V>(V)"
                  IL_006f:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                  IL_0074:  ldloc.s    V_4
                  IL_0076:  ret
                }
                """);
        }

        [Fact]
        public void InferredType_ExpressionElement()
        {
            string source = """
                using System.Collections.Generic;
                IDictionary<int, string> d = [new()];
                d.Report();
                d = [default];
                d.Report();
                """;
            var comp = CreateCompilation([source, s_dictionaryExtensions], options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "[0:null], [0:null], ");
            verifier.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var elements = tree.GetRoot().DescendantNodes().OfType<ExpressionElementSyntax>().ToArray();

            var typeInfo = model.GetTypeInfo(elements[0].Expression);
            Assert.Equal("System.Collections.Generic.KeyValuePair<System.Int32, System.String>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Collections.Generic.KeyValuePair<System.Int32, System.String>", typeInfo.ConvertedType.ToTestDisplayString());

            typeInfo = model.GetTypeInfo(elements[1].Expression);
            Assert.Equal("System.Collections.Generic.KeyValuePair<System.Int32, System.String>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Collections.Generic.KeyValuePair<System.Int32, System.String>", typeInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void InferredType_KeyValueElement_01()
        {
            string source = """
                using System.Collections.Generic;
                IDictionary<int, string> d = [default:default];
                d.Report();
                d = [new():null];
                d.Report();
                """;
            var comp = CreateCompilation([source, s_dictionaryExtensions], options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "[0:null], [0:null], ");
            verifier.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var elements = tree.GetRoot().DescendantNodes().OfType<KeyValuePairElementSyntax>().ToArray();

            var typeInfo = model.GetTypeInfo(elements[0].KeyExpression);
            Assert.Equal(SpecialType.System_Int32, typeInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_Int32, typeInfo.ConvertedType.SpecialType);
            typeInfo = model.GetTypeInfo(elements[0].ValueExpression);
            Assert.Equal(SpecialType.System_String, typeInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_String, typeInfo.ConvertedType.SpecialType);

            typeInfo = model.GetTypeInfo(elements[1].KeyExpression);
            Assert.Equal(SpecialType.System_Int32, typeInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_Int32, typeInfo.ConvertedType.SpecialType);
            typeInfo = model.GetTypeInfo(elements[1].ValueExpression);
            Assert.Null(typeInfo.Type);
            Assert.Equal(SpecialType.System_String, typeInfo.ConvertedType.SpecialType);
        }

        [Fact]
        public void InferredType_KeyValueElement_02()
        {
            string source = """
                using System.Collections.Generic;
                IDictionary<string, int> d = [null:new()];
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var elements = tree.GetRoot().DescendantNodes().OfType<KeyValuePairElementSyntax>().ToArray();

            var typeInfo = model.GetTypeInfo(elements[0].KeyExpression);
            Assert.Null(typeInfo.Type);
            Assert.Equal(SpecialType.System_String, typeInfo.ConvertedType.SpecialType);
            typeInfo = model.GetTypeInfo(elements[0].ValueExpression);
            Assert.Equal(SpecialType.System_Int32, typeInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_Int32, typeInfo.ConvertedType.SpecialType);
        }

        [Fact]
        public void ConversionError_ExpressionElement()
        {
            string source = """
                using System.Collections.Generic;
                var x = new object();
                IDictionary<int, int> d = [x, null];
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,28): error CS0029: Cannot implicitly convert type 'object' to 'System.Collections.Generic.KeyValuePair<int, int>'
                // IDictionary<int, int> d = [x, null];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("object", "System.Collections.Generic.KeyValuePair<int, int>").WithLocation(3, 28),
                // (3,31): error CS0037: Cannot convert null to 'KeyValuePair<int, int>' because it is a non-nullable value type
                // IDictionary<int, int> d = [x, null];
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("System.Collections.Generic.KeyValuePair<int, int>").WithLocation(3, 31));
        }

        [Fact]
        public void ConversionError_KeyValueElement_01()
        {
            string source = """
                using System.Collections.Generic;
                var x = new object();
                var y = new object();
                IDictionary<string, int> d = [x:1, "2":y];
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,31): error CS0029: Cannot implicitly convert type 'object' to 'string'
                // IDictionary<string, int> d = [x:1, "2":y];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("object", "string").WithLocation(4, 31),
                // (4,40): error CS0029: Cannot implicitly convert type 'object' to 'int'
                // IDictionary<string, int> d = [x:1, "2":y];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "y").WithArguments("object", "int").WithLocation(4, 40));
        }

        [Fact]
        public void ConversionError_KeyValueElement_02()
        {
            string source = """
                using System.Collections.Generic;
                IDictionary<string, int> d;
                d = [new():1];
                d = ["":null];
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,6): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                // d = [new():1];
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "new()").WithArguments("string", "0").WithLocation(3, 6),
                // (4,9): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                // d = ["":null];
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(4, 9));
        }

        [Fact]
        public void ConversionError_KeyValueElement_03()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        IDictionary<object, object> d = [P:P];
                    }
                    static object P { set { } }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,42): error CS0154: The property or indexer 'Program.P' cannot be used in this context because it lacks the get accessor
                //         IDictionary<object, object> d = [P:P];
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "P").WithArguments("Program.P").WithLocation(6, 42),
                // (6,44): error CS0154: The property or indexer 'Program.P' cannot be used in this context because it lacks the get accessor
                //         IDictionary<object, object> d = [P:P];
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "P").WithArguments("Program.P").WithLocation(6, 44));
        }

        [Fact]
        public void ConversionError_SpreadElement()
        {
            string source = """
                using System.Collections.Generic;
                IDictionary<string, int> d = [..new()];
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,33): error CS8754: There is no target type for 'new()'
                // IDictionary<string, int> d = [..new()];
                Diagnostic(ErrorCode.ERR_ImplicitObjectCreationNoTargetType, "new()").WithArguments("new()").WithLocation(2, 33));
        }

        [Theory]
        [InlineData("IDictionary")]
        [InlineData("IReadOnlyDictionary")]
        [InlineData("Dictionary")]
        public void TypeInference(string typeName)
        {
            string source = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Identity([default:default]);
                        Identity([default:"2"]);
                        Identity([1:default]);
                        Identity([1:default, default:"2"]);
                    }
                    static {{typeName}}<K, V> Identity<K, V>({{typeName}}<K, V> d) => d;
                }
                """;
            var comp = CreateCompilation(source);
            // PROTOTYPE: Type inference should succeed for Identity([1:default, default:"2"]);.
            comp.VerifyEmitDiagnostics(
                // (6,9): error CS0411: The type arguments for method 'Program.Identity<K, V>(IDictionary<K, V>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Identity([default:default]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Identity").WithArguments($"Program.Identity<K, V>(System.Collections.Generic.{typeName}<K, V>)").WithLocation(6, 9),
                // (7,9): error CS0411: The type arguments for method 'Program.Identity<K, V>(IDictionary<K, V>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Identity([default:"2"]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Identity").WithArguments($"Program.Identity<K, V>(System.Collections.Generic.{typeName}<K, V>)").WithLocation(7, 9),
                // (8,9): error CS0411: The type arguments for method 'Program.Identity<K, V>(IDictionary<K, V>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Identity([1:default]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Identity").WithArguments($"Program.Identity<K, V>(System.Collections.Generic.{typeName}<K, V>)").WithLocation(8, 9),
                // (9,9): error CS0411: The type arguments for method 'Program.Identity<K, V>(IDictionary<K, V>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         Identity([1:default, default:"2"]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Identity").WithArguments($"Program.Identity<K, V>(System.Collections.Generic.{typeName}<K, V>)").WithLocation(9, 9));
        }

        // PROTOTYPE: Test with [CollectionBuilder] type as well. In particular, should allow all three of [k:v, e, ..s] where Ke, Ve do not match K, V exactly.
        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        public void CustomDictionary_01(string typeKind)
        {
            string sourceA = $$"""
                using System.Collections;
                using System.Collections.Generic;
                public {{typeKind}} MyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
                {
                    private Dictionary<K, V> _dictionary;
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => GetDictionary().GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    public V this[K key]
                    {
                        get { return GetDictionary()[key]; }
                        set { GetDictionary()[key] = value; }
                    }
                    private Dictionary<K, V> GetDictionary() => _dictionary ??= new();
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();

            string sourceB = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Empty<string, int>().Report();
                        Many(1, "one", new KeyValuePair<int, string>(2, "two"), new KeyValuePair<int, string>[] { new(3, "three") }).Report();
                    }
                    static MyDictionary<K, V> Empty<K, V>() => [];
                    static MyDictionary<K, V> Many<K, V>(K k, V v, KeyValuePair<K, V> e, KeyValuePair<K, V>[] s) => /*<bind>*/[k:v, e, ..s]/*</bind>*/;
                }
                """;
            var verifier = CompileAndVerify(
                [sourceB, s_dictionaryExtensions],
                references: [refA],
                expectedOutput: "[], [1:one, 2:two, 3:three], ");
            verifier.VerifyDiagnostics();
            if (typeKind == "class")
            {
                verifier.VerifyIL("Program.Empty<K, V>", """
                    {
                      // Code size        6 (0x6)
                      .maxstack  1
                      IL_0000:  newobj     "MyDictionary<K, V>..ctor()"
                      IL_0005:  ret
                    }
                    """);
                verifier.VerifyIL("Program.Many<K, V>", """
                    {
                      // Code size       83 (0x53)
                      .maxstack  3
                      .locals init (MyDictionary<K, V> V_0,
                                    System.Collections.Generic.KeyValuePair<K, V> V_1,
                                    System.Collections.Generic.KeyValuePair<K, V>[] V_2,
                                    int V_3,
                                    System.Collections.Generic.KeyValuePair<K, V> V_4)
                      IL_0000:  newobj     "MyDictionary<K, V>..ctor()"
                      IL_0005:  stloc.0
                      IL_0006:  ldloc.0
                      IL_0007:  ldarg.0
                      IL_0008:  ldarg.1
                      IL_0009:  callvirt   "void MyDictionary<K, V>.this[K].set"
                      IL_000e:  ldarg.2
                      IL_000f:  stloc.1
                      IL_0010:  ldloc.0
                      IL_0011:  ldloca.s   V_1
                      IL_0013:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                      IL_0018:  ldloca.s   V_1
                      IL_001a:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                      IL_001f:  callvirt   "void MyDictionary<K, V>.this[K].set"
                      IL_0024:  ldarg.3
                      IL_0025:  stloc.2
                      IL_0026:  ldc.i4.0
                      IL_0027:  stloc.3
                      IL_0028:  br.s       IL_004b
                      IL_002a:  ldloc.2
                      IL_002b:  ldloc.3
                      IL_002c:  ldelem     "System.Collections.Generic.KeyValuePair<K, V>"
                      IL_0031:  stloc.s    V_4
                      IL_0033:  ldloc.0
                      IL_0034:  ldloca.s   V_4
                      IL_0036:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                      IL_003b:  ldloca.s   V_4
                      IL_003d:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                      IL_0042:  callvirt   "void MyDictionary<K, V>.this[K].set"
                      IL_0047:  ldloc.3
                      IL_0048:  ldc.i4.1
                      IL_0049:  add
                      IL_004a:  stloc.3
                      IL_004b:  ldloc.3
                      IL_004c:  ldloc.2
                      IL_004d:  ldlen
                      IL_004e:  conv.i4
                      IL_004f:  blt.s      IL_002a
                      IL_0051:  ldloc.0
                      IL_0052:  ret
                    }
                    """);
            }
            else
            {
                verifier.VerifyIL("Program.Empty<K, V>", """
                    {
                      // Code size       10 (0xa)
                      .maxstack  1
                      .locals init (MyDictionary<K, V> V_0)
                      IL_0000:  ldloca.s   V_0
                      IL_0002:  initobj    "MyDictionary<K, V>"
                      IL_0008:  ldloc.0
                      IL_0009:  ret
                    }
                    """);
                verifier.VerifyIL("Program.Many<K, V>", """
                    {
                      // Code size       88 (0x58)
                      .maxstack  3
                      .locals init (MyDictionary<K, V> V_0,
                                    System.Collections.Generic.KeyValuePair<K, V> V_1,
                                    System.Collections.Generic.KeyValuePair<K, V>[] V_2,
                                    int V_3,
                                    System.Collections.Generic.KeyValuePair<K, V> V_4)
                      IL_0000:  ldloca.s   V_0
                      IL_0002:  initobj    "MyDictionary<K, V>"
                      IL_0008:  ldloca.s   V_0
                      IL_000a:  ldarg.0
                      IL_000b:  ldarg.1
                      IL_000c:  call       "void MyDictionary<K, V>.this[K].set"
                      IL_0011:  ldarg.2
                      IL_0012:  stloc.1
                      IL_0013:  ldloca.s   V_0
                      IL_0015:  ldloca.s   V_1
                      IL_0017:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                      IL_001c:  ldloca.s   V_1
                      IL_001e:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                      IL_0023:  call       "void MyDictionary<K, V>.this[K].set"
                      IL_0028:  ldarg.3
                      IL_0029:  stloc.2
                      IL_002a:  ldc.i4.0
                      IL_002b:  stloc.3
                      IL_002c:  br.s       IL_0050
                      IL_002e:  ldloc.2
                      IL_002f:  ldloc.3
                      IL_0030:  ldelem     "System.Collections.Generic.KeyValuePair<K, V>"
                      IL_0035:  stloc.s    V_4
                      IL_0037:  ldloca.s   V_0
                      IL_0039:  ldloca.s   V_4
                      IL_003b:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                      IL_0040:  ldloca.s   V_4
                      IL_0042:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                      IL_0047:  call       "void MyDictionary<K, V>.this[K].set"
                      IL_004c:  ldloc.3
                      IL_004d:  ldc.i4.1
                      IL_004e:  add
                      IL_004f:  stloc.3
                      IL_0050:  ldloc.3
                      IL_0051:  ldloc.2
                      IL_0052:  ldlen
                      IL_0053:  conv.i4
                      IL_0054:  blt.s      IL_002e
                      IL_0056:  ldloc.0
                      IL_0057:  ret
                    }
                    """);
            }

            comp = (CSharpCompilation)verifier.Compilation;
            VerifyOperationTreeForTest<CollectionExpressionSyntax>(comp,
                """
                ICollectionExpressionOperation (3 elements, ConstructMethod: MyDictionary<K, V>..ctor()) (OperationKind.CollectionExpression, Type: MyDictionary<K, V>) (Syntax: '[k:v, e, ..s]')
                  Elements(3):
                      IOperation:  (OperationKind.None, Type: null) (Syntax: 'k:v')
                      IOperation:  (OperationKind.None, Type: null) (Syntax: 'e')
                      ISpreadOperation (ElementType: System.Collections.Generic.KeyValuePair<K, V>) (OperationKind.Spread, Type: null) (Syntax: '..s')
                        Operand:
                          IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Collections.Generic.KeyValuePair<K, V>[]) (Syntax: 's')
                        ElementConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          (Identity)
                """);
        }

        [Fact]
        public void Lock()
        {
            string source = """
                using System.Collections.Generic;
                using System.Threading;
                class Program
                {
                    static void Main()
                    {
                        var x = new Lock();
                        var y = new Lock();
                        object[] a = [x];
                        IDictionary<object, object> d = [x:y];
                    }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyEmitDiagnostics(
                // (9,23): warning CS9216: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
                //         object[] a = [x];
                Diagnostic(ErrorCode.WRN_ConvertingLock, "x").WithLocation(9, 23),
                // (10,42): warning CS9216: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
                //         IDictionary<object, object> d = [x:y];
                Diagnostic(ErrorCode.WRN_ConvertingLock, "x").WithLocation(10, 42),
                // (10,44): warning CS9216: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
                //         IDictionary<object, object> d = [x:y];
                Diagnostic(ErrorCode.WRN_ConvertingLock, "y").WithLocation(10, 44));
        }
    }
}
